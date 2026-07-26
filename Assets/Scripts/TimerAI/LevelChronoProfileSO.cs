using UnityEngine;

[CreateAssetMenu(fileName = "New Chrono Profile", menuName = "StopMotion/Level Chrono Profile")]
public class LevelChronoProfileSO : ScriptableObject
{
    [System.Serializable]
    public struct ChronoBeat
    {
        [Tooltip("Minimum duration of this beat, in seconds.")]
        [Min(0.05f)]
        public float minDuration;

        [Tooltip("Maximum duration of this beat, in seconds.")]
        [Min(0.05f)]
        public float maxDuration;

        [Tooltip("True = player must MOVE during this beat (drives Ticking). " +
                 "False = player must FREEZE during this beat (drives Frozen).")]
        public bool isMoveBeat;
    }

    [Header("Scripted Sequence")]
    [Tooltip("Ordered list of move/stop beats that make up this level's rhythm. " +
             "Played back in order by AIEnemyInfluence.")]
    public ChronoBeat[] beats;

    [Header("Playback")]
    [Tooltip("If true, the sequence loops back to beat 0 after the last beat.")]
    public bool loopSequence = true;

    public float TotalDuration()
    {
        float total = 0f;
        if (beats == null) return 0f;
        foreach (var beat in beats)
        {
            total += Random.Range(beat.minDuration, beat.maxDuration);
        }
        return total;
    }
}
