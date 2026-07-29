import csv
import itertools
import os
import random

# Your four conditions
conditions = ["AdapativeArchery", "NonAdaptiveArchery", "AdaptivePuzzle", "NonAdaptivePuzzle"]

# File to store assignments
FILENAME = "participant_assignments.csv"

# Generate all 24 orderings
all_orders = list(itertools.permutations(conditions))

# Create the file if it doesn't exist
if not os.path.exists(FILENAME):
    with open(FILENAME, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["PID", "OrderIndex", "ConditionOrder"])

# Read previous assignments
assigned_pids = set()
counts = [0] * len(all_orders)

with open(FILENAME, newline="") as f:
    reader = csv.DictReader(f)
    for row in reader:
        assigned_pids.add(int(row["PID"]))
        counts[int(row["OrderIndex"])] += 1

# Find orderings with the fewest participants
min_count = min(counts)
available = [i for i, c in enumerate(counts) if c == min_count]

# Randomly choose one of the least-used orderings
order_index = random.choice(available)
condition_order = all_orders[order_index]

# Generate a unique 9-digit PID
while True:
    pid = random.randint(100_000_000, 999_999_999)
    if pid not in assigned_pids:
        break

# Save assignment
with open(FILENAME, "a", newline="") as f:
    writer = csv.writer(f)
    writer.writerow([
        pid,
        order_index,
        ",".join(condition_order)
    ])

print("Participant ID:", pid)
print("Condition order:", condition_order)