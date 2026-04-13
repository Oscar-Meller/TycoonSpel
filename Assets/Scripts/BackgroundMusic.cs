using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance;

    [Tooltip("Audio clips that will be played. Playlist loops when finished.")]
    public AudioClip[] playlist;

    [Tooltip("Automatically start playing on Start")]
    public bool playOnStart = true;

    [Range(0f, 1f)]
    [Tooltip("Master volume for music")]
    public float volume = 0.5f; // Default to 0.5

    [Tooltip("If true this GameObject will survive scene loads")]
    public bool dontDestroyOnLoad = false;

    [Tooltip("Shuffle the playlist order")]
    public bool shuffle = true; // default to shuffled order

    [Tooltip("Start muted")]
    public bool startMuted = false;

    AudioSource audioSource;

    // playOrder maps play index -> playlist index
    int[] playOrder;
    int currentIndex = 0;
    Coroutine playlistCoroutine;
    bool isMuted = false;

    void Awake()
    {
        // singleton to avoid duplicate players with different settings
        if (Instance != null && Instance != this)
        {
            Debug.Log("[BackgroundMusic] Another instance exists - destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false; // we manage looping the playlist ourselves
        audioSource.spatialBlend = 0f; // 2D music by default

        // apply inspector values (ensure applied even if AudioSource inspector differs)
        audioSource.volume = Mathf.Clamp01(volume);
        isMuted = startMuted;
        audioSource.mute = isMuted;

        Debug.Log($"[BackgroundMusic] Awake. volume={audioSource.volume}, muted={audioSource.mute}, playOnAwake={audioSource.playOnAwake}");

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[BackgroundMusic] DontDestroyOnLoad enabled.");
        }

        var listener = FindObjectOfType<AudioListener>();
        if (listener == null)
        {
            Debug.LogWarning("[BackgroundMusic] No AudioListener found in scene. Add one (usually on the Main Camera).");
        }

        // Prepare a play order now so the first track is randomized when playOnStart is true.
        BuildPlayOrder();
    }

    void Start()
    {
        // Re-apply volume in Start to guard against runtime changes by other systems
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.mute = isMuted;
        }

        Debug.Log($"[BackgroundMusic] Start. playOnStart={playOnStart}, playlist length={(playlist == null ? 0 : playlist.Length)}, effectiveVolume={audioSource.volume}");

        if (playlist == null || playlist.Length == 0)
        {
            Debug.LogWarning("[BackgroundMusic] Playlist is empty — nothing will play.");
            return;
        }

        if (playOnStart)
            PlayPlaylist();
    }

    void Update()
    {
        // Toggle mute with the M key
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMute();
    }

    void OnValidate()
    {
        // Keep inspector changes in sync while editing
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.mute = isMuted;
        }
    }

    // Public control API
    public void PlayPlaylist()
    {
        if (playlist == null || playlist.Length == 0)
        {
            Debug.LogWarning("[BackgroundMusic] PlayPlaylist called but playlist is empty.");
            return;
        }

        Debug.Log("[BackgroundMusic] PlayPlaylist called.");
        StopPlaylist();

        // Rebuild order in case shuffle setting changed at runtime
        BuildPlayOrder();
        currentIndex = 0;

        // enforce volume before starting
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.mute = isMuted;
        }

        playlistCoroutine = StartCoroutine(PlaylistLoop());
    }

    public void StopPlaylist()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
            playlistCoroutine = null;
            Debug.Log("[BackgroundMusic] Playlist coroutine stopped.");
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[BackgroundMusic] AudioSource stopped.");
        }
    }

    public void PlayNext()
    {
        if (playlist == null || playlist.Length == 0) return;
        currentIndex = (currentIndex + 1) % playOrder.Length;
        PlayCurrentClip();
    }

    public void PlayPrevious()
    {
        if (playlist == null || playlist.Length == 0) return;
        currentIndex = (currentIndex - 1 + playOrder.Length) % playOrder.Length;
        PlayCurrentClip();
    }

    public void PlayIndex(int index)
    {
        if (playlist == null || playlist.Length == 0) return;
        currentIndex = Mathf.Clamp(index, 0, playOrder.Length - 1);
        PlayCurrentClip();
    }

    void PlayCurrentClip()
    {
        if (playlist == null || playlist.Length == 0) return;

        int playlistIndex = playOrder[currentIndex];
        var clip = playlist[playlistIndex];
        if (clip == null)
        {
            Debug.LogWarning($"[BackgroundMusic] Clip at playlist index {playlistIndex} is null.");
            return;
        }

        // ensure correct volume/mute before playing
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.mute = isMuted;
        }

        audioSource.clip = clip;
        audioSource.Play();
        Debug.Log($"[BackgroundMusic] Playing clip {currentIndex} (playlist index {playlistIndex}): {clip.name} (length {clip.length:F2}s), volume={audioSource.volume}, mute={audioSource.mute}");
    }

    IEnumerator PlaylistLoop()
    {
        if (playlist == null || playlist.Length == 0)
            yield break;

        while (true)
        {
            int playlistIndex = playOrder[currentIndex];

            // Skip null clips
            if (playlist[playlistIndex] == null)
            {
                Debug.LogWarning($"[BackgroundMusic] Skipping null clip at playlist index {playlistIndex}.");
                currentIndex = (currentIndex + 1) % playOrder.Length;
                yield return null;
                continue;
            }

            // ensure correct volume/mute before playing
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Clamp01(volume);
                audioSource.mute = isMuted;
            }

            audioSource.clip = playlist[playlistIndex];
            audioSource.Play();
            Debug.Log($"[BackgroundMusic] Coroutine playing clip {currentIndex} (playlist index {playlistIndex}): {audioSource.clip.name}");

            // Wait until the clip finishes playing
            yield return new WaitWhile(() => audioSource.isPlaying);

            // Advance to next clip; wrap around at end to loop the playlist
            currentIndex = (currentIndex + 1) % playOrder.Length;
        }
    }

    // Build playOrder array according to shuffle setting.
    // Uses a time-based seed so order varies between runs.
    void BuildPlayOrder()
    {
        int n = (playlist == null) ? 0 : playlist.Length;
        playOrder = new int[n];
        for (int i = 0; i < n; i++)
            playOrder[i] = i;

        if (shuffle && n > 1)
        {
            // Fisher-Yates shuffle using System.Random seeded by time + instance id
            int seed = Environment.TickCount ^ GetInstanceID();
            var rnd = new System.Random(seed);

            for (int i = n - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int tmp = playOrder[i];
                playOrder[i] = playOrder[j];
                playOrder[j] = tmp;
            }

            Debug.Log("[BackgroundMusic] Playlist shuffled.");
        }
    }

    // Simple on-screen mute toggle with shortcut label (shows (M))
    void OnGUI()
    {
        const int width = 110;
        const int height = 28;
        Rect rect = new Rect(Screen.width - width - 10, 10, width, height);

        string label = isMuted ? "Unmute (M)" : "Mute (M)";
        if (GUI.Button(rect, label))
        {
            ToggleMute();
        }

        // Small hint text under the button (short)
        Rect hintRect = new Rect(Screen.width - width - 10, 10 + height + 4, width, 18);
        GUI.Label(hintRect, "Toggle music with M");
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        if (audioSource != null)
            audioSource.mute = isMuted;

        Debug.Log($"[BackgroundMusic] Mute toggled. nowMuted={isMuted}");
    }
}

// (unchanged helper example)
public class MusicControlExample : MonoBehaviour
{
    public BackgroundMusic musicPlayer; // assign in inspector

    // Hook this to a UI Button to skip to next track
    public void OnNextPressed()
    {
        if (musicPlayer != null)
            musicPlayer.PlayNext();
    }

    // Example to start/stop
    public void OnTogglePlayPressed()
    {
        if (musicPlayer == null) return;

        // Toggle play/stop
        if (musicPlayer != null && musicPlayer.GetComponent<AudioSource>().isPlaying)
            musicPlayer.StopPlaylist();
        else
            musicPlayer.PlayPlaylist();
    }
}
