# SoundManager API 说明

> 基于 `Assets/Scripts/WManager/Sound/SoundManager.cs` 与 `Audio.cs` 当前源码。
> 静态单例管理器，统一管理 Music / Eff / UISound 三类音频：播放、暂停/恢复、停止、淡入淡出、完成回调、对象池与全局音量。

## 1. 能力概览

- `PrepareMusic/Eff/UISound` 仅初始化音频对象，不自动播放。
- `PlayMusic/Eff/UISound` 一次性 Prepare + Play。
- 音乐默认淡入淡出 1 秒；音效与 UI 音效默认 0 秒淡入淡出。
- 重复播放策略由 `IgnoreDuplicateMusic/Effs/UISounds` 控制。
- 支持 3D 音频（提供 `Transform sourceTransform`）。
- 全部为静态方法，调用前无需实例化（首次访问 `Instance` 时自动创建 `SoundManager` 单例 GameObject）。

## 2. 全局音量

~~~csharp
SoundManager.GlobalVolume         // 总音量
SoundManager.GlobalMusicVolume    // 音乐
SoundManager.GlobalEffsVolume     // 特效
SoundManager.GlobalUISoundsVolume // UI 音效

SoundManager.IgnoreDuplicateMusic    = true;
SoundManager.IgnoreDuplicateEffs     = true;
SoundManager.IgnoreDuplicateUISounds = true;
~~~

## 3. 播放音乐

~~~csharp
int musicId = SoundManager.PlayMusic(clip);
int musicId = SoundManager.PlayMusic(clip, volume: 0.8f);
int musicId = SoundManager.PlayMusic(clip, volume: 0.8f, loop: true, persist: true);
int musicId = SoundManager.PlayMusic(clip, volume: 0.8f, loop: true, persist: true,
                                     fadeInSeconds: 1f, fadeOutSeconds: 1f);
int musicId = SoundManager.PlayMusic(clip, volume: 0.8f, loop: true, persist: true,
                                     fadeInSeconds: 1f, fadeOutSeconds: 1f,
                                     currentMusicfadeOutSeconds: 0.5f, sourceTransform: emitter);
~~~

- 播放音乐时自动调用 `StopAllMusic(currentMusicfadeOutSeconds)`。
- `currentMusicfadeOutSeconds = -1` 时保持当前音乐自身的淡出时间。
- `sourceTransform` 不为空时启用 3D 音频。

## 4. 播放特效与 UI 音效

~~~csharp
int effId = SoundManager.PlayEff(clip);
int effId = SoundManager.PlayEff(clip, volume: 0.6f);
int effId = SoundManager.PlayEff(clip, loop: true);
int effId = SoundManager.PlayEff(clip, volume: 0.6f, loop: true, sourceTransform: emitter);

int uiId = SoundManager.PlayUISound(clip);
int uiId = SoundManager.PlayUISound(clip, volume: 0.6f);
~~~

> 特效与 UI 音效默认不淡入淡出；如需淡入淡出可先 `PrepareXxx` 后再调用 `Audio.SetVolume(volume, fadeSeconds)`。

## 5. Prepare（仅初始化，不播放）

`PrepareMusic/Eff/UISound` 的重载与 `PlayXxx` 完全一致，但不播放；常用于先准备好背景音乐等待时机播放。

~~~csharp
int id = SoundManager.PrepareMusic(clip, volume: 1f, loop: true, persist: true,
                                   fadeInSeconds: 1f, fadeOutSeconds: 1f);
SoundManager.SetAudioFinishedCallback(id, OnFinished);
~~~

## 6. 音频实例查询

~~~csharp
Audio audio = SoundManager.GetAudio(audioId);
Audio audio = SoundManager.GetAudio(clip);
Audio music = SoundManager.GetMusicAudio(idOrClip);
Audio eff   = SoundManager.GetEffAudio(idOrClip);
Audio ui    = SoundManager.GetUISoundAudio(idOrClip);

bool restored = SoundManager.RestoreAudioFromPool(Audio.AudioType.Music, id);
~~~

`Audio` 对象暴露的属性：`AudioID`、`Clip`、`Volume`、`IsPlaying`、`Paused`、`Stopping`、`Activated`、`Pooled`、`Loop`、`Mute`、`Priority`、`Pitch`、`StereoPan`、`SpatialBlend`、`ReverbZoneMix`、`DopplerLevel`、`Spread`、`Min3DDistance`、`Max3DDistance`、`Persist`、`FadeInSeconds`、`FadeOutSeconds`；方法：`Play/Play(volume)`、`Stop`、`Pause`、`UnPause`、`Resume`、`SetVolume(volume)`、`SetVolume(volume, fadeSeconds)`、`SetVolume(volume, fadeSeconds, startVolume)`、`Set3DDistances(min, max)`、`Update`。

## 7. 暂停 / 恢复 / 停止

~~~csharp
SoundManager.PauseAll();
SoundManager.PauseAllMusic();
SoundManager.PauseAllEff();
SoundManager.PauseAllUISounds();

SoundManager.ResumeAll();
SoundManager.ResumeAllMusic();
SoundManager.ResumeAllEff();
SoundManager.ResumeAllUISounds();

SoundManager.StopAll();                  // 全部停止，音乐使用自身淡出
SoundManager.StopAll(musicFadeOutSeconds: 1f);
SoundManager.StopAllMusic();
SoundManager.StopAllMusic(fadeOutSeconds: 1f);
SoundManager.StopAllEff();
SoundManager.StopAllUISounds();
~~~

`StopAllAudio` 实现会先复制字典 keys，避免在遍历时修改。`fadeOutSeconds <= 0` 时使用音频自身的淡出时间。

## 8. 完成回调

~~~csharp
SoundManager.SetAudioFinishedCallback(audioId, args =>
{
    // args: Audio.AudioFinishedCallback
});
SoundManager.SetAudioFinishedCallback(clip, args => { });
~~~

`Audio.AudioFinishedCallback` 委托类型定义在 `Audio.cs` 中，可直接传入 lambda。

## 9. 使用注意事项

- 默认 `IgnoreDuplicate*` 全部为 `false`；如果希望同一 clip 重复播放，保持默认；如希望复用现有 Audio，置 `true`。
- `persist = true` 时音频源在场景切换后保留。
- 3D 音频需要设置 `Min3DDistance` 与 `Max3DDistance`（默认在 `Audio` 内提供）。
- 单例在销毁时会清空池中 Audio；如果业务需要长期持有，请自行保存 ID。
- `RestoreAudioFromPool` 用于把对象池中的 Audio 重新激活；如果传入的 ID 不在池中，返回 `false`。
- 全局音量（`GlobalVolume` 等）通过 `VolumeController` 设置更推荐；`GlobalVolumeController` 是供 Inspector/UnityEvent 使用的组件封装。
