# SoundManager Audio API 说明

> 配套 `SoundManager说明API.md`。本文件说明 `Audio` 实例与音量控制层。

## 1. Audio 对象

每个通过 `PrepareXxx` / `PlayXxx` 创建的音频都对应一个 `Audio` 实例，可通过 `SoundManager.GetAudio/GetMusicAudio/GetEffAudio/GetUISoundAudio` 取得。

### 1.1 主要属性

| 属性 | 说明 |
|---|---|
| `int AudioID` | 唯一标识 |
| `AudioClip Clip` | 当前音频 |
| `float Volume` | 当前音量（受全局音量影响） |
| `bool IsPlaying / Paused / Stopping / Activated / Pooled` | 状态 |
| `bool Loop / Mute` | 循环、静音 |
| `int Priority` | AudioSource 优先级 |
| `float Pitch / StereoPan / SpatialBlend / ReverbZoneMix / DopplerLevel / Spread` | 3D 与音效参数 |
| `float Min3DDistance / Max3DDistance` | 3D 距离 |
| `bool Persist` | 是否跨场景保留 |
| `float FadeInSeconds / FadeOutSeconds` | 淡入淡出时间 |

### 1.2 主要方法

~~~csharp
audio.Play();
audio.Play(0.5f);                       // 覆盖当前音量

audio.Stop();
audio.Pause();
audio.UnPause();                       // 取消暂停并继续
audio.Resume();                        // 等价 UnPause

audio.SetVolume(0.5f);                 // 立即设音量
audio.SetVolume(0.5f, fadeSeconds: 1f);
audio.SetVolume(0.5f, fadeSeconds: 1f, startVolume: 0.8f);

audio.Set3DDistances(min: 1f, max: 50f);
audio.Update();                        // 由 SoundManager 每帧驱动
~~~

## 2. VolumeController

`VolumeController` 是按通道读取全局音量的静态封装。提供：

- `VolumeController.Global` / `Music` / `Eff` / `UISound`（可读可写）。
- `event VolumeChanged(SoundChannel)`：任意通道变更时触发。
- `ResetToDefault()`：恢复所有通道默认值。

枚举 `SoundChannel`：

| 值 | 说明 |
|---|---|
| `Global` | 总音量 |
| `Music` | 音乐通道 |
| `Eff` | 特效通道 |
| `UISound` | UI 音效通道 |

写入任一通道时，`SoundManager` 的全局音量属性会同步生效。

## 3. GlobalVolumeController 组件

`GlobalVolumeController` 是 MonoBehaviour 组件，可挂到场景物体上供 UnityEvent 调用：

- 字段 `GVolume`、`GMusicVolume`、`GEffVolume`、`GUIVolume` 用于初始化。
- 提供方法：`GlobalVolume(v)`、`GlobalMusicVolume(v)`、`GlobalEffVolume(v)`、`GlobalUIVolume(v)`，内部直接写到 `SoundManager` 全局音量。
- Inspector 中可将 `On Value Changed` 之类事件连入这些方法。

## 4. 使用示例

~~~csharp
int id = SoundManager.PlayMusic(bgmClip, volume: 0.6f, loop: true, persist: true);
Audio audio = SoundManager.GetMusicAudio(id);
audio.SetVolume(0.3f, fadeSeconds: 1f); // 1 秒淡出到 0.3

VolumeController.Music = 0.8f;
VolumeController.VolumeChanged += channel => Debug.Log($"通道 {channel} 已变更");

// 预制 Slider 控制全局音量
slider.onValueChanged.AddListener(GlobalVolumeController.GlobalVolume);
~~~

## 5. 注意事项

- `Audio.Update` 由 `SoundManager` 内部驱动；不要在外部手动 `Update`。
- 全局音量与各通道音量相乘生效；通过 `VolumeController` 修改会触发 `VolumeChanged` 事件。
- `SetVolume(volume, fadeSeconds, startVolume)` 同时指定起始音量，常用于从非当前音量开始淡入淡出。
- `Set3DDistances` 生效的前提是 SpatialBlend > 0；2D 音频无需设置。
- `Persist=true` 时 AudioSource GameObject 会通过 `DontDestroyOnLoad` 跨场景保留。
