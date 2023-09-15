# 纹路风 » SoundManager 说明 API

## 简介

**SoundManager**使用时不用挂载任何物体,引用 WManager 后即可调用播放指定的任意音频。

### 实现原理

要播放音频时,会创建一个空物体,上方挂载**AudioSources**组件,所有 2D 音频都在这个空物体下播放,该空物体可设置为切换场景不删除,以保证切换场景仍然有声音,所有创建的声音都会有独一无二的 ID,以字典的方式存放在**Audio**中,方便随时调用。也可传入**transform**,则此音频将会放到此物体坐标下成为 3D 音频播放。

所有音频播放一次后会立马删除,不会占用内存。

### 使用注意事项

使用**SoundManager**需调用命名空间: **using WManager**;

*SoundManager*这个物体会在第一次调用**SoundManager**(即第一次播放声音)时创建。

这个新的**GameObject**保存着使用管理器时生成的所有 2D**AudioSource**组件。

不要在**SoundManager**脚本之外与这些**AudioSources**交互!

## 使用方法

你可以使用 3 个不同的声音组,_Music_,*Eff*和*UISound*。

### 播放 Music 和 Eff

使用注意:要播放新音频,只需使用 PlayMusic 播放背景音乐,使用 PlayEff 播放声音,使用 PlayUISound 播放 UI 声音。
这些函数返回一个唯一的 AudioID,之后可将其用于访问创建的 Audio 对象。注:默认情况下播放新的 Music 音频将停止上一个。

```csharp
// 导入命名空间用于访问SoundManager。
using WManager;
public class Demo : MonoBehaviour
{
  void Start()
  {
    // 1直接播放名为(musicAudioClip)的音频
    SoundManager.PlayMusic(musicAudioClip);
    // 2设置更多参数,直接在后面点就行
    int backgroundMusicID = SoundManager.PlayMusic(musicAudioClip, 0.7f, true, false, 1, 1);
  }
}
```

在上面的例子中

1 将直接播放名为**musicAudioClip**的音频,默认音量为 1,只播放一次,不会在场景中持续播放,具有 1 秒的淡入淡出过渡。

2 将会播放名为**musicAudioClip**的音频,音量为 0.7,将循环播放,不会在场景中持续播放,并具有 1 秒的淡入淡出过渡。
并返回一个名为**backgroundMusicID**的 ID,通过该 ID 我们可以找到改播放此音频的**AudioSources**组件,并对此组件进行设置。

### 准备 Audio 对象

也可以在不立即播放音频的情况下创建和初始化音频。
在需要事先准备和创建所有或部分音频并稍后使用的情况下使用。
只需使用任何准备函数并保存在**Audio**对象中即可。

```csharp
// 导入命名空间用于访问SoundManager。
using WManager;
public class Demo : MonoBehaviour
{
  // 创建Audio对象
  public Audio fightSceneAudio;

  void Start()
  {
    // 准备名为(fightSceneAudioClip)的音频,并返回该音频唯一ID
    int fightSceneMusicID = SoundManager.PrepareMusic(fightSceneAudioClip, 1f, true,false, 0.5f, 1);
    // 将ID为(fightSceneMusicID)的音频赋值给fightSceneAudio
    fightSceneAudio = SoundManager.GetAudio(fightSceneMusicID);
    // 播放该Audio对象
    fightSceneAudio.Play();
  }
}
```

### 访问 Audio 对象

每个音频对象都有自己的 AudioID。此 ID 可用于在创建后随时访问。并可以单独播放、停止或暂停。

```csharp
Audio backgroundMusicAudio = SoundManager.GetAudio(backgroundMusicID);
backgroundMusicAudio.Stop();
```

如果已经知道音频的类型,还可以使用特定类型的 GetAudio 函数:GetMusicAudio、GetSoundAudio 和 GetUISoundAudio。
另一种方法是使用 AudioClip 进行搜索

```csharp
public AudioClip backgroundMusicClip;
...
Audio backgroundMusicAudio = SoundManager.GetAudio(backgroundMusicClip);
//或者
Audio backgroundMusicAudio = SoundManager.GetMusicAudio(backgroundMusicClip);
backgroundMusicAudio.Stop();
```

此外,通过访问音频对象,可以更改淡入/淡出速度、音频音量或是否保持循环等设置。

```csharp
// 将fightSceneAudio当做**AudioSources**组件去使用其中的方法即可
backgroundMusicAudio.SetVolume(0.5f);
backgroundMusicAudio.Loop = false;
// 更多方法看下面API
```

### 3D 音频

Sound Manager 支持播放 3D(空间)音频。
与 2D 音频的区别是:需要指定一个 transform 作为音频的源(如果不需要 3D 音频,使用 null 即可,默认就是 null)。

```csharp
int gunShootSoundID = SoundManager.PlaySound(gunShootClip, 1f, false, gunTransform);
```

可以通过 Audio 对象修改 3D 音频设置。如最大距离,最小距离,空间混合,衰减模式等。

```csharp
Audio gunShootAudio = SoundManager.GetAudio(gunShootSoundID);
gunShootAudio.Min3DDistance = 1f;
gunShootAudio.Max3DDistance = 10f;
gunShootAudio.SpatialBlend = 0.8f;
gunShootAudio.RolloffMode = AudioRolloffMode.Linear;
```

### 全局音量

参看 Sound 文件夹的 GlobalVolumeController.cs,我做成了预制体,也可以用 Slider 绑定相应方法调节。

### 重复播放

在默认设置下,如果播放已经播放的音频,不会被再次播放。
若想重复播放音频,只需将任何音频类型的 IgnoreDucplicate 选项设置为 True 即可。

```csharp
SoundManager.IgnoreDuplicateMusic = True;
SoundManager.IgnoreDuplicateEffs = True;
SoundManager.IgnoreDuplicateUISounds = True;
```

## 属性

| 名称                    | 类型       | 说明                                                                   |
| ----------------------- | ---------- | ---------------------------------------------------------------------- |
| Gameobject              | gameobject | 声音管理器附加到的游戏对象                                             |
| GlobalMusicVolume       | float      | 全局音乐音量                                                           |
| GlobalEffsVolume        | float      | 全局效果音量                                                           |
| GlobalUISoundsVolume    | float      | 全局 UI 音量                                                           |
| GlobalVolume            | float      | 全局总音量                                                             |
| IgnoreDuplicateMusic    | bool       | 当设置为 true 时,若使用相同的音频(AudioClip)播放新音乐则将会被忽略     |
| IgnoreDuplicateEffs     | bool       | 当设置为 true 时,若使用相同的音频(AudioClip)播放新特效音则将会被忽略   |
| IgnoreDuplicateUISounds | bool       | 当设置为 true 时,若使用相同的音频(AudioClip)播放新 UI 声音则将会被忽略 |

## 方法

| 名称                 | 传值                                                                                                                                                                                     | 说明                                                                                             |
| -------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| GetAudio             | Int32                                                                                                                                                                                    | 如果找到 audioID,则返回其 id 为 audioID 的音频,如果未找到此类音频,则返回 null                    |
| GetAudio             | AudioClip                                                                                                                                                                                | 返回播放给定音频的第一次播放的音频。如果未找到此类音频,则返回 null                               |
| GetMusicAudio        | Int32                                                                                                                                                                                    | 如果找到 audioID,则返回其 id 为 audioID 的音频,如果未找到此类音频,则返回 null                    |
| GetMusicAudio        | AudioClip                                                                                                                                                                                | 返回播放给定音频的第一次播放的音频。如果未找到此类音频,则返回 null                               |
| GetEffAudio          | Int32                                                                                                                                                                                    | 如果找到 audioID,则返回其 id 为 audioID 的音频,如果未找到此类音频,则返回 null                    |
| GetEffAudio          | AudioClip                                                                                                                                                                                | 返回播放给定音频的第一次播放的音频。如果未找到此类音频,则返回 null                               |
| GetUISoundAudio      | Int32                                                                                                                                                                                    | 如果找到 audioID,则返回其 id 为 audioID 的音频,如果未找到此类音频,则返回 null                    |
| GetUISoundAudio      | AudioClip                                                                                                                                                                                | 返回播放给定音频的第一次播放的音频。如果未找到此类音频,则返回 null                               |
| PauseAll             | void                                                                                                                                                                                     | 暂停所有声音                                                                                     |
| PauseAllMusic        | void                                                                                                                                                                                     | 暂停所有音乐                                                                                     |
| PauseAllEff          | void                                                                                                                                                                                     | 暂停所有效果音                                                                                   |
| PauseAllUISounds     | void                                                                                                                                                                                     | 暂停所有 UI 音效                                                                                 |
| PlayMusic            | <li>AudioClip</li><li>AudioClip,float</li><li>AudioClip,float,bool,bool</li><li>AudioClip,float,bool,bool,float,float</li><li>AudioClip,float,bool,bool,float,float,float,Transform</li> | 播放指定的背景音乐,音量,循环,更改场景不删除,淡入时间,淡出时间,本音频淡出时间,3D 音频位置         |
| PlayEff              | <li>AudioClip</li><li>AudioClip,float</li><li>AudioClip,bool</li><li>AudioClip,float,bool,Transform</li>                                                                                 | 播放指定的特效声音,音量,循环,3D 音频位置                                                         |
| PlayUISound          | <li>AudioClip</li><li>AudioClip,float</li>                                                                                                                                               | 播放指定的 UI 声音,音量                                                                          |
| PrepareMusic         | <li>AudioClip</li><li>AudioClip,float</li><li>AudioClip,float,bool,bool</li><li>AudioClip,float,bool,bool,float,float</li><li>AudioClip,float,bool,bool,float,float,float,Transform</li> | 准备及初始化指定的背景音乐,音量,循环,更改场景不删除,淡入时间,淡出时间,本音频淡出时间,3D 音频位置 |
| PrepareEff           | <li>AudioClip</li><li>AudioClip,float</li><li>AudioClip,bool</li><li>AudioClip,float,bool,Transform</li>                                                                                 | 准备及初始化指定的特效声音,音量,循环,3D 音频位置                                                 |
| PrepareUISound       | <li>AudioClip</li><li>AudioClip,float</li>                                                                                                                                               | 准备及初始化指定的 UI 声音,音量                                                                  |
| RestoreAudioFromPool | Audio.AudioType,float                                                                                                                                                                    | 还原并将合并的音频重新添加到其对应的音频字典中                                                   |
| ResumeAll            | void                                                                                                                                                                                     | 恢复所有音频播放                                                                                 |
| ResumeAllMusic       | void                                                                                                                                                                                     | 恢复所有音乐播放                                                                                 |
| ResumeAllEff         | void                                                                                                                                                                                     | 恢复所有特效音播放                                                                               |
| ResumeAllUISounds    | void                                                                                                                                                                                     | 恢复所有 UI 音播放                                                                               |
| StopAll              | <li>void</li><li>float</li>                                                                                                                                                              | 停止所有音频播放,并设定淡出时间                                                                  |
| StopAllMusic         | <li>void</li><li>float</li>                                                                                                                                                              | 停止所有背景音播放,并设定淡出时间                                                                |
| StopAllEff           | void                                                                                                                                                                                     | 停止所有特效音播放                                                                               |
| StopAllUISounds      | void                                                                                                                                                                                     | 停止所有 UI 音播放                                                                               |
