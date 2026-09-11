using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeEncounterValidation
{
    // Runs against the saved scene and its real baked NavMesh, without entering Play.
    public static void Run()
    {
        Directory.CreateDirectory("Logs");
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PrototypeArena.unity");
            var encounter = UnityEngine.Object.FindObjectOfType<Tools.ArenaEncounterManager>();
            var player = UnityEngine.Object.FindObjectOfType<Player.PlayerController>();
            if (encounter == null || player == null) throw new Exception("Missing arena or player.");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = encounter.GetType();
            type.GetField("_spawnOrigin", flags).SetValue(encounter, player.transform.position);
            Vector3 facing = player.FacingDirection;
            type.GetField("_spawnForward", flags).SetValue(encounter, facing);
            var positions = new List<Vector3>();
            bool found = (bool)type.GetMethod("FindSpawnPositions", flags).Invoke(encounter, new object[] { 10, positions });
            if (!found || positions.Count != 10) throw new Exception($"Only {positions.Count}/10 reachable positions found.");
            for (int i = 0; i < positions.Count; i++)
            {
                if (Vector3.Dot(positions[i] - player.transform.position, facing) < 12f)
                    throw new Exception("An enemy is too close to the starting area.");
                for (int j = 0; j < i; j++)
                    if (Vector3.ProjectOnPlane(positions[i] - positions[j], Vector3.up).magnitude < 8f)
                        throw new Exception("Enemy spacing is below eight units.");
            }
            File.WriteAllText("Logs/EncounterValidation.txt", "PASS: 10 reachable positions ahead of spawn; initial buffer and separation verified.\n" +
                string.Join("\n", positions));
        }
        catch (Exception exception)
        {
            File.WriteAllText("Logs/EncounterValidation.txt", "FAILED: " + exception);
            Debug.LogException(exception);
        }
    }
}
