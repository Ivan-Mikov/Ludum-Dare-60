using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Статическая ссылка на единственный экземпляр (Синглтон)
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

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

    // Перечисления для удобного вызова из других скриптов
    public enum MusicTrack { Normal, Boss }
    public enum SoundEffect { CannonShot, Ricochet, Laser, Error, Victory, BossDamage, EnemyDeath }

    private void Awake()
    {
        // Реализация паттерна Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Объект не уничтожится при смене сцен
            
            // Автоматически добавляем AudioSource, если забыли прикрепить в инспекторе
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

        // Настройки по умолчанию для музыки
        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }

    /// <summary>
    /// Включает указанный саундтрек. Автоматически зациклен.
    /// </summary>
    public void PlayMusic(MusicTrack track)
    {
        AudioClip selectedTrack = track switch
        {
            MusicTrack.Normal => normalMusic,
            MusicTrack.Boss => bossMusic,
            _ => null
        };

        if (selectedTrack == null)
        {
            Debug.LogWarning($"Саундтрек {track} не назначен в инспекторе SoundManager!");
            return;
        }

        // Если эта музыка уже играет, ничего не делаем
        if (musicSource.clip == selectedTrack && musicSource.isPlaying) return;

        musicSource.clip = selectedTrack;
        musicSource.Play();
    }

    /// <summary>
    /// Останавливает фоновую музыку.
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>
    /// Воспроизводит звуковой эффект один раз. Звуки могут накладываться друг на друга.
    /// </summary>
    public void PlaySFX(SoundEffect effect)
    {
        AudioClip selectedClip = effect switch
        {
            SoundEffect.CannonShot => cannonShotSound,
            SoundEffect.Ricochet => ricochetSound,
            SoundEffect.Laser => laserSound,
            SoundEffect.Error => errorSound,
            SoundEffect.Victory => victorySound,
            SoundEffect.BossDamage => bossDamageSound,
            SoundEffect.EnemyDeath => enemyDeathSound,
            _ => null
        };

        if (selectedClip != null)
        {
            // PlayOneShot позволяет звукам накладываться друг на друга без прерывания
            sfxSource.PlayOneShot(selectedClip);
        }
        else
        {
            Debug.LogWarning($"Звуковой эффект {effect} не назначен в инспекторе SoundManager!");
        }
    }
}
