using UnityEngine;

// The 24 proper rotations of a cube
public static class CubeRotations
{
    public static readonly Quaternion[] All = BuildAll();

    static Quaternion[] BuildAll()
    {
        Vector3[] ups = {
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back,
            Vector3.left, Vector3.right
        };

        var rotations = new System.Collections.Generic.List<Quaternion>();

        foreach (Vector3 up in ups)
        {
            Vector3 forward = Vector3.Cross(up, Vector3.right);
            if (forward.sqrMagnitude < 0.5f)
                forward = Vector3.Cross(up, Vector3.forward);
            forward.Normalize();

            for (int i = 0; i < 4; i++)
            {
                Quaternion spin = Quaternion.AngleAxis(90f * i, up);
                Quaternion rot = Quaternion.LookRotation(spin * forward, up);
                rotations.Add(rot);
            }
        }

        return rotations.ToArray();
    }

    // Given any held rotation, returns the one of the 24 valid cube cloests fit
    public static Quaternion SnapToNearest(Quaternion held)
    {
        Quaternion best = All[0];
        float bestAngle = Quaternion.Angle(held, All[0]);

        for (int i = 1; i < All.Length; i++)
        {
            float angle = Quaternion.Angle(held, All[i]);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = All[i];
            }
        }

        return best;
    }
}