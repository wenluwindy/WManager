# SoundManagerAudio 说明 API

## 简介

这个类被**SoundManager**脚本用来包装 Unity [AudioClip](https://docs.unity3d.com/ScriptReference/AudioClip.html) 类,并添加了更多的功能。一般对 AudioClip 的操作都要通过这个类来调用。

```csharp
// 引用WManager命名空间
using WManager;

public class Demo : MonoBehaviour
{
  public AudioClip BGM;

  void Start()
  {
    // 准备一个背景音
    int ID = SoundManager.PrepareMusic(BGM);
    // 创建一个SoundManagerAudio,并将id为ID的音频赋值给它
    Audio audio = SoundManager.GetAudio(ID);
    //播放该音乐
    audio.Play();

    // 打印出要播放的audio的名字、是否正在播放、播放时长
    Debug.Log(audio.Clip.name);
    Debug.Log(audio.IsPlaying);
    Debug.Log(audio.Clip.length);
  }

  // 一个调用后既能暂停,又能播放的事件
  public void PlayPauseBGM()
  {
    // 找到名为BGM的AudioClip
    var a = SoundManager.GetMusicAudio(BGM);
    // 判断其是否为空与是否正在播放
    if (a != null && a.IsPLaying)
    {
      // 暂停音乐
      a.Pause();
    }
    else
    {
      // 播放音乐
      a.Play();
    }
  }

}
```

## 属性

| 名称            | 类型             | 说明                                                                                                 |
| --------------- | ---------------- | ---------------------------------------------------------------------------------------------------- |
| Activated       | bool             | 是否至少创建和更新一次音频。                                                                         |
| AudioID         | int              | Audio 的 ID                                                                                          |
| AudioSource     | AudioSource      | 负责此音频的音频源。不可修改,请改用 Audio 类。                                                       |
| Clip            | AudioClip        | 要播放/正在播放的音频                                                                                |
| DopplerLevel    | float            | 音频的多普勒程度                                                                                     |
| FadeInSeconds   | float            | 淡入时间                                                                                             |
| FadeOutSeconds  | float            | 淡出时间                                                                                             |
| IsPlaying       | bool             | 是否正在播放                                                                                         |
| Max3DDistance   | float            | (默认对数衰减)声音停止衰减的最大距离。                                                               |
| Min3DDistance   | float            | (默认对数衰减)声音停止衰减的最小距离。                                                               |
| Mute            | bool             | 音频是否静音                                                                                         |
| Paused          | bool             | 音频是否暂停                                                                                         |
| Persist         | bool             | 场景更改之间音频是否持续存在                                                                         |
| Pitch           | float            | 音频的音调                                                                                           |
| Priority        | int              | 设置音频的优先级                                                                                     |
| ReverbZoneMix   | float            | 来自 AudioSource 的信号将混合到与混响区相关的全局混响中的量。                                        |
| RolloffMode     | AudioRolloffMode | 音频如何随距离衰减                                                                                   |
| SourceTransform | Transform        | 音频的位置                                                                                           |
| SpatialBlend    | float            | 设置此 AudioSource 受 3D 空间化计算(衰减、多普勒等)的影响程度。0.0 表示声音为 2D,1.0 表示声音为 3D。 |
| Spread          | float            | 扬声器空间中三维立体声或多声道声音的扩散角(以度为单位)。                                             |
| StereoPan       | float            | 以立体声方式(向左或向右)平移播放声音。这仅适用于单声道或立体声的声音。                               |
| Stopping        | bool             | 音频是否停止                                                                                         |
| Type            | Audio. AudioType | 音频的类型                                                                                           |
| Volume          | bool             | 音频的音量                                                                                           |

## 方法

| 名称           | 传值              | 说明                                                                                                     |
| -------------- | ----------------- | -------------------------------------------------------------------------------------------------------- |
| Pause          | void              | 暂停                                                                                                     |
| Play()         | void              | 播放                                                                                                     |
| Play()         | float             | 以设置的音量播放                                                                                         |
| Resume         | void              | 继续播放                                                                                                 |
| Set3DDistances | void              | 设置音频 3D 距离                                                                                         |
| SetVolume      | float             | 设置音量                                                                                                 |
| SetVolume      | float,float       | 设置音量,设置淡化时间                                                                                    |
| SetVolume      | float,float,float | 设置音量,设置淡化时间,在开始淡入之前,立即将音量设置为该值。如果未通过,音频将开始从当前音量向目标音量衰减 |
| Stop           | void              | 停止播放                                                                                                 |
| UnPause        | void              | 继续播放,和上面的效果一样                                                                                |
| Update         | void              | 音频的更新循环。这是声音管理器本身自动调用的。不要调用                                                   |
