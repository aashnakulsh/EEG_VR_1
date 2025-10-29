from ortools.sat.python import cp_model
import sys
import json
import argparse

def generate_trials_ortools(num_cubes=5,
                            total_trials=100,
                            target_ratio=0.2,
                            no_ghost_start_trials=5,
                            min_target_gap=2,
                            max_target_gap=6,
                            n=3,
                            max_time_seconds=300,
                            workers=8):
    """
    OR-Tools CP-SAT generator mirroring your C# GenerateTrials() constraints.
    Returns a list of (cube_index, is_target) of length total_trials.
    """
    model = cp_model.CpModel()
    num_targets = round(total_trials * target_ratio)
    num_non_targets = total_trials - num_targets

    # Variables
    cube = [model.NewIntVar(0, num_cubes - 1, f"cube_{t}") for t in range(total_trials)]
    is_target = [model.NewBoolVar(f"is_target_{t}") for t in range(total_trials)]

    # Reified per-cube booleans: is_cube[t][c] <-> cube[t] == c
    is_cube = [[model.NewBoolVar(f"is_cube_{t}_{c}") for c in range(num_cubes)]
               for t in range(total_trials)]
    for t in range(total_trials):
        model.Add(sum(is_cube[t][c] for c in range(num_cubes)) == 1)
        for c in range(num_cubes):
            model.Add(cube[t] == c).OnlyEnforceIf(is_cube[t][c])
            model.Add(cube[t] != c).OnlyEnforceIf(is_cube[t][c].Not())

    # 1) exact number of targets
    model.Add(sum(is_target) == num_targets)

    # 2) no targets at start
    for t in range(min(no_ghost_start_trials, total_trials)):
        model.Add(is_target[t] == 0)

    # 3) min gap between targets: forbid targets closer than min_target_gap
    for t in range(total_trials):
        for dt in range(1, min_target_gap + 1):
            if t + dt < total_trials:
                model.AddBoolOr([is_target[t].Not(), is_target[t + dt].Not()])

    # 4) max gap between targets: forbid runs of (max_target_gap+1) consecutive non-targets
    if max_target_gap >= 0:
        window_len = max_target_gap + 1
        if window_len <= total_trials:
            for start in range(0, total_trials - window_len + 1):
                window = [is_target[start + i] for i in range(window_len)]
                model.Add(sum(window) >= 1)

    # 5) no >2 consecutive targets
    if total_trials >= 3:
        for start in range(0, total_trials - 3 + 1):
            model.Add(sum(is_target[start + i] for i in range(3)) <= 2)

    # 6) Blocking after target: if s is target and uses cube c, then for next n trials cube[t] != c
    for s in range(total_trials):
        for future_offset in range(1, n + 1):
            t = s + future_offset
            if t < total_trials:
                for c in range(num_cubes):
                    model.Add(is_cube[t][c] == 0).OnlyEnforceIf([is_target[s], is_cube[s][c]])

    # 7) If t is a target and uses cube c, then that cube c must NOT have been used in previous n trials
    for t in range(total_trials):
        for past_offset in range(1, n + 1):
            s = t - past_offset
            if s >= 0:
                for c in range(num_cubes):
                    model.Add(is_cube[s][c] == 0).OnlyEnforceIf([is_target[t], is_cube[t][c]])

    # 7b) Previous-ghost safety: the cube used by the *immediately previous* target
    # must NOT appear in the n trials immediately before the current target.
    # We model this by selecting, for each non-first target t, exactly one s in
    # [t - max_target_gap, t - min_target_gap] that is the previous target and
    # ensuring that cube[s] is absent in (t-n .. t-1).

    prevpair = [[None for _ in range(total_trials)] for __ in range(total_trials)]

    for t in range(total_trials):
        # Candidates for previous target index s for this t (bounded by gap constraints)
        cand_s = list(range(max(0, t - max_target_gap), max(-1, t - min_target_gap) + 1))
        # Boolean indicating whether no prior targets exist before t
        zero_prev_t = model.NewBoolVar(f"zero_prev_before_{t}")
        prev_count = sum(is_target[u] for u in range(0, t))
        if t == 0:
            model.Add(prev_count == 0)
            model.Add(zero_prev_t == 1)
        else:
            model.Add(prev_count == 0).OnlyEnforceIf(zero_prev_t)
            model.Add(prev_count >= 1).OnlyEnforceIf(zero_prev_t.Not())

        first_target_t = model.NewBoolVar(f"first_target_{t}")
        # first_target_t = is_target[t] AND zero_prev_t
        model.Add(first_target_t <= is_target[t])
        model.Add(first_target_t <= zero_prev_t)
        model.Add(first_target_t >= is_target[t] + zero_prev_t - 1)

        b_list = []
        for s in cand_s:
            if s < 0 or s >= t:
                continue
            b = model.NewBoolVar(f"prevpair_{s}_{t}")
            prevpair[s][t] = b

            b_list.append(b)
            # b implies s and t are consecutive targets (no targets in between)
            model.Add(is_target[s] == 1).OnlyEnforceIf(b)
            model.Add(is_target[t] == 1).OnlyEnforceIf(b)
            if s + 1 <= t - 1:
                model.Add(sum(is_target[u] for u in range(s + 1, t)) == 0).OnlyEnforceIf(b)
            # If b holds, forbid using cube[s] in the last n trials before t
            for j in range(max(0, t - n), t):
                for c in range(num_cubes):
                    model.Add(is_cube[j][c] == 0).OnlyEnforceIf([b, is_cube[s][c]])

        if b_list:
            # Exactly one previous-target candidate must be chosen for non-first targets,
            # and none for first targets or non-target trials:
            # sum(b_list) == is_target[t] - first_target_t
            sum_b = sum(b_list)
            model.Add(sum_b <= is_target[t] - first_target_t)
            model.Add(sum_b >= is_target[t] - first_target_t)

    # Prevents 3 target trials of the same index in a row
    # it’s not allowed that (s is prev of u) and (u is prev of t) and all three use cube c.
    for t in range(total_trials):
        for u in range(t):
            bu = prevpair[u][t]
            if bu is None:
                continue
            for s in range(u):
                bs = prevpair[s][u]
                if bs is None:
                    continue
                # For each cube c, forbid bs & bu & (cube[s]==c) & (cube[u]==c) & (cube[t]==c)
                for c in range(num_cubes):
                    model.AddBoolOr([
                        bu.Not(), bs.Not(),
                        is_cube[s][c].Not(),
                        is_cube[u][c].Not(),
                        is_cube[t][c].Not()
                    ])

    # Rule 7c: No more than 2 consecutive non-targets at the same cube position 
    for c in range(num_cubes):
        for t in range(2, total_trials):  # start from t=2 so we can look back two trials
            # Boolean conditions for three consecutive non-targets at the same cube
            conds = [
                is_cube[t - 2][c],
                is_cube[t - 1][c],
                is_cube[t][c],
                is_target[t - 2].Not(),
                is_target[t - 1].Not(),
                is_target[t].Not()
            ]

            # OR-Tools constraint: at least one of these must be false
            # (so you can't have all six true at once)
            model.AddBoolOr([cond.Not() for cond in conds])

    # 8) Linearize target_on_cube and non_target_on_cube booleans
    target_on = [[model.NewBoolVar(f"target_on_{t}_{c}") for c in range(num_cubes)]
                 for t in range(total_trials)]
    non_target_on = [[model.NewBoolVar(f"nontarget_on_{t}_{c}") for c in range(num_cubes)]
                     for t in range(total_trials)]
    for t in range(total_trials):
        for c in range(num_cubes):
            model.Add(target_on[t][c] <= is_target[t])
            model.Add(target_on[t][c] <= is_cube[t][c])
            model.Add(target_on[t][c] >= is_target[t] + is_cube[t][c] - 1)

            model.Add(non_target_on[t][c] <= is_target[t].Not())
            model.Add(non_target_on[t][c] <= is_cube[t][c])
            model.Add(non_target_on[t][c] >= is_cube[t][c] - is_target[t])

    # 9) Balance targets and non-targets per cube (each within +/-1)
    # 9) Enforce that per-cube target and non-target counts differ by at most 1 globally
    target_counts = []
    non_target_counts = []

    for c in range(num_cubes):
        tcount = model.NewIntVar(0, num_targets, f"target_count_{c}")
        ncount = model.NewIntVar(0, num_non_targets, f"nontarget_count_{c}")
        model.Add(tcount == sum(target_on[t][c] for t in range(total_trials)))
        model.Add(ncount == sum(non_target_on[t][c] for t in range(total_trials)))
        target_counts.append(tcount)
        non_target_counts.append(ncount)

    t_max = model.NewIntVar(0, num_targets, "t_max")
    t_min = model.NewIntVar(0, num_targets, "t_min")
    n_max = model.NewIntVar(0, num_non_targets, "n_max")
    n_min = model.NewIntVar(0, num_non_targets, "n_min")

    model.AddMaxEquality(t_max, target_counts)
    model.AddMinEquality(t_min, target_counts)
    model.Add(t_max - t_min <= 1)

    model.AddMaxEquality(n_max, non_target_counts)
    model.AddMinEquality(n_min, non_target_counts)
    model.Add(n_max - n_min <= 1)


    # Symmetry breaking (optional): fix first trial cube to 0 to help solver
    model.Add(cube[0] == 0)

    # Objective: minimize total deviation from ideal per-cube total (speeds finding balanced solution)
    total_on_cube = []
    ideal_total = total_trials // num_cubes
    for c in range(num_cubes):
        tc = model.NewIntVar(0, total_trials, f"total_on_cube_{c}")
        model.Add(tc == sum(is_cube[t][c] for t in range(total_trials)))
        dev = model.NewIntVar(0, total_trials, f"dev_{c}")
        model.Add(tc - ideal_total <= dev)
        model.Add(ideal_total - tc <= dev)
        total_on_cube.append(dev)

    model.Minimize(sum(total_on_cube))

    # Solve
    solver = cp_model.CpSolver()
    solver.parameters.max_time_in_seconds = max_time_seconds
    solver.parameters.num_search_workers = workers
    solver.parameters.random_seed = 1

    status = solver.Solve(model)
    if status not in (cp_model.OPTIMAL, cp_model.FEASIBLE):
        sys.stderr.write("❌ No valid sequence found within time limit.")
        return []

    seq = [(solver.Value(cube[t]), bool(solver.Value(is_target[t]))) for t in range(total_trials)]

    # Print per-cube stats for quick check
    targ_counts = [sum(int(solver.Value(target_on[t][c])) for t in range(total_trials)) for c in range(num_cubes)]
    non_targ_counts = [sum(int(solver.Value(non_target_on[t][c])) for t in range(total_trials)) for c in range(num_cubes)]
    total_counts = [targ_counts[c] + non_targ_counts[c] for c in range(num_cubes)]
    # print(f"Total trials: {total_trials}")
    # print(f"Target trials: {num_targets}")
    # print(f"Non-target trials: {num_non_targets}\n")
    # print("Target trials per index:")
    # for c in range(num_cubes):
    #     print(f"  index {c}: {targ_counts[c]}")
    # print("\nNon-target trials per index:")
    # for c in range(num_cubes):
    #     print(f"  index {c}: {non_targ_counts[c]}")
    # print("\nTotal trials per index:")
    # for c in range(num_cubes):
    #     print(f"  index {c}: {total_counts[c]}")
    # sys.stdout.write(json.dumps([{"cubeIndex": c, "isTarget": t} for c, t in seq]))
    # sys.stdout.flush()


    return seq


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--num_cubes", type=int, default=5)
    parser.add_argument("--total_trials", type=int, default=1000)
    parser.add_argument("--target_ratio", type=float, default=0.2)
    parser.add_argument("--no_ghost_start_trials", type=int, default=5)
    parser.add_argument("--min_target_gap", type=int, default=2)
    parser.add_argument("--max_target_gap", type=int, default=6)
    parser.add_argument("--n", type=int, default=3)
    parser.add_argument("--max_time_seconds", type=int, default=30)
    args = parser.parse_args()

    seq = generate_trials_ortools(
        num_cubes=args.num_cubes,
        total_trials=args.total_trials,
        target_ratio=args.target_ratio,
        no_ghost_start_trials=args.no_ghost_start_trials,
        min_target_gap=args.min_target_gap,
        max_target_gap=args.max_target_gap,
        n=args.n,
        max_time_seconds=args.max_time_seconds
    )

    # Output as JSON list of {cubeIndex, isTarget} for Unity
    
    # print(json.dumps([{"cubeIndex": c, "isTarget": t} for c, t in seq]))
    print(json.dumps([{"cubeIndex": c, "isTarget": t} for c, t in seq]))
    sys.stdout.flush()


# # Example usage
# if __name__ == "__main__":
#     seq = generate_trials_ortools(num_cubes=5,
#                                   total_trials=1000,
#                                   target_ratio=0.2,
#                                   no_ghost_start_trials=5,
#                                   min_target_gap=2,
#                                   max_target_gap=6,
#                                   n=3,
#                                   max_time_seconds=20,
#                                   workers=8)
#     # print("\nFirst 40 trials (cube, is_target):")
#     # for i, s in enumerate(seq[:40]):
#         # print(i, s)
#     print(seq)
