using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class VolumeManager : MonoBehaviour
{
    [Header("BGM Settings")]
    [SerializeField] private AudioMixer bgmMixer;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;

    [Header("SE Settings")]
    [SerializeField] private AudioMixer seMixer;
    [SerializeField] private Slider seSlider;
    [SerializeField] private TextMeshProUGUI seValueText;

    // AudioMixerで公開するパラメータ名（Inspectorで設定したものと一致させる）
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SE_VOLUME_PARAM = "SEVolume";

    // PlayerPrefsで保存する際のキー
    private const string BGM_VOLUME_KEY = "BGM_Volume_Key";
    private const string SE_VOLUME_KEY = "SE_Volume_Key";

    private void Start()
    {
        // --- 保存された音量設定を読み込む ---
        // BGM：キーが存在すればその値を、なければデフォルト値0.5を読み込む
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        // SE：キーが存在すればその値を、なければデフォルト値0.5を読み込む
        float seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);

        // --- UIとAudioMixerの初期値を設定 ---
        // スライダーの初期値を設定
        bgmSlider.value = bgmVolume;
        seSlider.value = seVolume;
        
        // AudioMixerとテキスト表示を更新
        UpdateBGMVolume(bgmVolume);
        UpdateSEVolume(seVolume);

        // --- スライダーの値が変更されたときのイベントリスナーを登録 ---
        bgmSlider.onValueChanged.AddListener(UpdateBGMVolume);
        seSlider.onValueChanged.AddListener(UpdateSEVolume);
    }

    /// <summary>
    /// BGMの音量を更新し、設定を保存する
    /// </summary>
    /// <param name="volume">スライダーの値 (0.0 ~ 1.0)</param>
    private void UpdateBGMVolume(float volume)
    {
        // テキスト表示を更新 (0 ~ 100の整数)
        bgmValueText.text = Mathf.RoundToInt(volume * 100).ToString();

        // AudioMixerの値を更新（リニア値をデシベルに変換）
        // 0のときは-80dB（ミュート）にする
        bgmMixer.SetFloat(BGM_VOLUME_PARAM, volume == 0 ? -80f : Mathf.Log10(volume) * 20);

        // 設定を保存
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
    }

    /// <summary>
    /// SEの音量を更新し、設定を保存する
    /// </summary>
    /// <param name="volume">スライダーの値 (0.0 ~ 1.0)</param>
    private void UpdateSEVolume(float volume)
    {
        // テキスト表示を更新 (0 ~ 100の整数)
        seValueText.text = Mathf.RoundToInt(volume * 100).ToString();

        // AudioMixerの値を更新（リニア値をデシベルに変換）
        seMixer.SetFloat(SE_VOLUME_PARAM, volume == 0 ? -80f : Mathf.Log10(volume) * 20);

        // 設定を保存
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, volume);
    }
}