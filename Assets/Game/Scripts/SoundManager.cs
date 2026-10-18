using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource laserSource;

    [Header("Soundtracks")]
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip bossMusic;

    [Header("Sound Effects (SFX)")]
    [SerializeField] private AudioClip cannonShotSound;
    [SerializeField] private AudioClip ricochetSound;
    [SerializeField] private AudioClip laserSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip bossDamageSound;
    [SerializeField] private AudioClip enemyDeathSound;

    // --- ИНДИВИДУАЛЬНАЯ ГРОМКОСТЬ (Можно ставить больше 1.0f!) ---
    [Header("Volume Settings (Can be > 1)")]
    public float volNormalMusic = 1f;
    public float volBossMusic = 1f;
    
    public float volCannonShot = 2f;
    public float volRicochet = 1f;
    public float volLaser = 0.5f;
    public float volError = 3f;
    public float volVictory = 1f;
    public float volBossDamage = 0.2f;
    public float volEnemyDeath = 0.2f;

    public enum MusicTrack { Normal, Boss }
    public enum SoundEffect { CannonShot, Ricochet, Error, Victory, BossDamage, EnemyDeath }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ValidateAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ValidateAudioSources()
    {
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (laserSource == null) laserSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        laserSource.loop = true;
        laserSource.playOnAwake = false;
        if (laserSound != null) laserSource.clip = laserSound;
    }

    // --- МЕТОДЫ ДЛЯ ИЗМЕНЕНИЯ ИЗ КОДА ---
    
    public void SetVolume(SoundEffect effect, float volume)
    {
        switch (effect)
        {
            case SoundEffect.CannonShot: volCannonShot = volume; break;
            case SoundEffect.Ricochet: volRicochet = volume; break;
            case SoundEffect.Error: volError = volume; break;
            case SoundEffect.Victory: volVictory = volume; break;
            case SoundEffect.BossDamage: volBossDamage = volume; break;
            case SoundEffect.EnemyDeath: volEnemyDeath = volume; break;
        }
        
        // Если лазер играет прямо сейчас, обновляем его громкость в реальном времени
        if (effect == SoundEffect.CannonShot && laserSource.isPlaying) laserSource.volume = volLaser; 
    }

    public void SetMusicVolume(MusicTrack track, float volume)
    {
        if (track == MusicTrack.Normal) volNormalMusic = volume;
        else volBossMusic = volume;

        // Если этот трек сейчас играет, сразу меняем ему громкость
        if (musicSource.isPlaying)
        {
            if (track == MusicTrack.Normal && musicSource.clip == normalMusic) musicSource.volume = volNormalMusic;
            if (track == MusicTrack.Boss && musicSource.clip == bossMusic) musicSource.volume = volBossMusic;
        }
    }

    // --- УПРАВЛЕНИЕ ЛАЗЕРОМ ---
    public void OnLaser()
    {
        if (laserSound == null) return;
        if (laserSource.clip != laserSound) laserSource.clip = laserSound;
        
        laserSource.volume = volLaser; // Применяем личную громкость лазера
        if (!laserSource.isPlaying) laserSource.Play();
    }

    public void OffLaser()
    {
        if (laserSource.isPlaying) laserSource.Stop();
    }

    // --- ВОСПРОИЗВЕДЕНИЕ МУЗЫКИ ---
    public void PlayMusic(MusicTrack track)
    {
        AudioClip selectedTrack = null;
        float selectedVolume = 1f;

        if (track == MusicTrack.Normal)
        {
            selectedTrack = normalMusic;
            selectedVolume = volNormalMusic;
        }
        else if (track == MusicTrack.Boss)
        {
            selectedTrack = bossMusic;
            selectedVolume = volBossMusic;
        }

        if (selectedTrack == null || (musicSource.clip == selectedTrack && musicSource.isPlaying)) return;

        musicSource.clip = selectedTrack;
        musicSource.volume = selectedVolume; // Применяем личную громкость трека
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // --- ВОСПРОИЗВЕДЕНИЕ ЭФФЕКТОВ ---
    public void PlaySFX(SoundEffect effect)
    {
        AudioClip clip = null;
        float volume = 1f;

        switch (effect)
        {
            case SoundEffect.CannonShot: clip = cannonShotSound; volume = volCannonShot; break;
            case SoundEffect.Ricochet: clip = ricochetSound; volume = volRicochet; break;
            case SoundEffect.Error: clip = errorSound; volume = volError; break;
            case SoundEffect.Victory: clip = victorySound; volume = volVictory; break;
            case SoundEffect.BossDamage: clip = bossDamageSound; volume = volBossDamage; break;
            case SoundEffect.EnemyDeath: clip = enemyDeathSound; volume = volEnemyDeath; break;
        }

        if (clip != null)
        {
            // Воспроизводим конкретный звук с его личной громкостью
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
