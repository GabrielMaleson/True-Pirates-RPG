using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Singleton music manager that persists across scenes.
/// Handles looping background music and exposes Yarn commands:
///   <<playmusic "trackName">>  — plays a named track from the musicTracks list
///   <<stopmusic>>              — stops current music
///
/// Also called directly by CombatSystem to play battleMusic from an EncounterFile.
///
/// Setup: Place this on a persistent GameObject in your starting scene.
///        Assign AudioSource and populate musicTracks in the inspector.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [System.Serializable]
    public class MusicEntry
    {
        public string musicName;
        public AudioClip clip;
    }

    [Header("Named Music Tracks")]
    public List<MusicEntry> musicTracks = new List<MusicEntry>();

    [Header("Audio Source")]
    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Play an AudioClip directly (used by CombatSystem for battleMusic).
    /// Does nothing if the clip is null or already playing.
    /// </summary>
    public void PlayClip(AudioClip clip)
    {
        if (clip == null) { Debug.Log("[MusicManager] PlayClip ignorado — clip é null."); return; }
        if (audioSource.clip == clip && audioSource.isPlaying) { Debug.Log($"[MusicManager] PlayClip ignorado — '{clip.name}' já está tocando."); return; }

        Debug.Log($"[MusicManager] Tocando: '{clip.name}'");
        audioSource.clip = clip;
        audioSource.Play();
    }

    /// <summary>
    /// Stop the currently playing music.
    /// </summary>
    public void StopMusic()
    {
        Debug.Log($"[MusicManager] StopMusic — clip anterior: '{(audioSource.clip != null ? audioSource.clip.name : "nenhum")}'");
        audioSource.Stop();
        audioSource.clip = null;
    }

    // ── Yarn Commands ──────────────────────────────────────────────────────

    /// <summary>
    /// Yarn: <<playmusic "trackName">>
    /// Plays a track from the musicTracks list by name.
    /// </summary>
    [YarnCommand("playmusic")]
    public static void PlayMusicCommand(string musicName)
    {
        if (Instance == null)
        {
            Debug.LogError("MusicManager: No instance found. Add a MusicManager to your scene.");
            return;
        }

        MusicEntry found = Instance.musicTracks.Find(m => m.musicName == musicName);
        if (found == null || found.clip == null)
        {
            Debug.LogError($"MusicManager: No track named '{musicName}' found. Check musicTracks in the inspector.");
            return;
        }

        Instance.PlayClip(found.clip);
    }

    /// <summary>
    /// Yarn: <<stopmusic>>
    /// Stops whatever is currently playing.
    /// </summary>
    [YarnCommand("stopmusic")]
    public static void StopMusicCommand()
    {
        if (Instance == null) return;
        Instance.StopMusic();
    }
}
