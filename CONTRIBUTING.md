# 開発ガイドライン

## 環境

- Unity: 2022.3.62f

## Assetsのフォルダ構成

**コンフリクトを避けるため、各ミニゲームの作業は必ず自分のミニゲームタイトルのフォルダの中に格納してください。**
既に作業を始めている人も、このフォルダ構成に合わせてファイルを移動してください。

```
Assets
├─ Scripts
│  ├─ サップのミニゲームの仮タイトル
│  │  └─ xxx_PlayerController.cs
│  └─ むむむのミニゲームの仮タイトル
│     └─ mmm_PlayerController.cs
└─ Prefabs
   ├─ サップのミニゲームの仮タイトル
   │  └─ xxx_Player.prefab
   └─ むむむのミニゲームの仮タイトル
      └─ mmm_Player.prefab
```

- Scripts, Prefabs それぞれの直下にミニゲームタイトルのフォルダを作る
- その中に各々の作業内容を格納する
- ファイル名は `ミニゲームタイトルの略称_○○○` の形式にする（例: `xxx_PlayerController.cs`, `mmm_Player.prefab`）
- SoundManagerなど複数のミニゲームで共有するスクリプトはミニゲームフォルダに入れず、`Scripts` 直下に置く

## 命名規則

ぐちゃぐちゃになっていなければOK。以下のどちらかで統一してください。

- PascalCase
- snake_case

## Git

### コミット

- コミットは基本的にタスク単位で行う
  - 例: プレイヤー操作を作ったらコミット、UIを作ったらコミット
  - イメージとしてはTMS一個終えるごと
  - コミット数が多すぎるとリポジトリからバージョンを追跡するのが大変になるため
- ただしタスクが大きい場合など、作業が途中でも適切なタイミングがあればコミットして良い（各自の裁量）

### コミットメッセージ

`<type>: <内容>` の形式で書く。

| type | 内容 |
| --- | --- |
| feat | 新機能に関する変更 |
| fix | バグ修正 |
| docs | ドキュメンテーションの変更 |
| refactor | リファクタリング |
| test | テストの追加や修正 |
| setting | Project settingの変更など |

例:
- `feat: プレイヤー操作実装`
- `fix: スコアの計算処理`

余裕があれば [Gitmoji](https://gitmoji.dev/) を使ってもOK。
例: `git commit -m "fix: 🐛 ○○エラー修正"`

### ブランチ

- 作業ブランチは必ず `main` から自分のあだ名で派生させる

### プルリクエスト

プルリクのメッセージには以下を書く。

- 行った作業
- 行っていない作業
- 気になっていること
- 不具合（あれば）

プルリクから12時間経ってもマージされなかったら、Discordでもう一度リマインドする。

### マージ

- 基本セルフマージなし。他の誰かにチェックしてもらってからマージする

## 音

**音までつけて完成とします。** 効果音・BGMは自作のコンテナ式サウンドマネージャー（`Assets/Scripts/SoundManager.cs`）を使ってください。

### はじめに

通常、UnityのAudio Sourceで音を再生する場合、1つのAudio Sourceにつき1つの音声ファイルの再生・ストップが可能です。

この方法でも音の再生は可能ですが、例えば以下の状況などで不便です。

- 状況に応じて複数のBGMやSEを使い分ける
- 様々なスクリプトから音を再生する
- ゲーム内の設定画面やオプションで音量調整などを行う

そこで、それら音に関する処理を一括で管理できるマネージャーを作りました。

このサウンドマネージャーを使えば、**どのスクリプトからでも**以下のコード1行でBGMやSEの再生が可能です。

```csharp
SoundManager.Instance.PlayBGM(SoundManager.BGMData.BGMTYPE.Title);
SoundManager.Instance.PlaySE(SoundManager.SEData.SETYPE.NormalButton);
```

### 使い方

#### 1. SoundManager オブジェクトを作成する

まず、タイトルシーンに空の GameObject を作成し、名前を **SoundManager** にします。

その後、`SoundManager` スクリプトをアタッチしてください。

> **注意**
> SoundManager は基本的に1つだけ使います。他のシーンに同じものを重複して置かないようにしてください。
> 興味があれば後述するシングルトンの解説で説明します。

#### 2. Audio Sourceを追加する

SoundManager オブジェクトに Audio Source を**2つ** Add Componentしましょう。

理由は、BGMとSEをそれぞれ流すためです。

- BGM: 長く流し続ける音
- SE: UIへのタッチ時や攻撃時など、何度も短く鳴る音

Audio Source 1つだとBGMを流しながらSEを流すことなどができません。BGM流す用とSE流す用のスピーカーを用意するようなイメージです。

SoundManager内のAudio Sourceの参照にそれぞれアタッチして、Audio Sourceの準備は完了です。基本的にはそのまま使えますが、必要に応じて `Play On Awake` や `Loop` などを調整してください。

#### 3. 音声ファイルの登録

フリー音源などで、ゲームの雰囲気にあったBGM、SEを用意しましょう。それをサウンドマネージャーで再生できるようにします。

SoundManagerの中の `Bgm Sound Datas` を開いて「＋」を押すと要素が追加されます。各データでは、以下を設定します。

- 種類（enum）
- AudioClip
- 音量

enumはゲームごとに、使用するBGM, SEに合った名前をコードに記述してください。SEの場合も同様に、`Se Sound Datas` に登録しましょう。

#### 4. 再生方法

簡単です。

```csharp
SoundManager.Instance.PlayBGM(SoundManager.BGMData.BGMTYPE.Title);
SoundManager.Instance.PlaySE(SoundManager.SEData.SETYPE.NormalButton);
```

例えば、

- シーン開始時にタイトルBGMを流す
- ボタンを押したときにSEを鳴らす
- プレイヤーが攻撃したときに攻撃SEを鳴らす

といった使い方ができ、複数人で開発するときにも便利です。

#### 5. 音量調整

今はゲーム中のいつでも上下の矢印キーを押すと調整できるようになっています。ここは仮の実装なので、今の調整している数値を参考に、必要に応じてたとえば以下のように変更してください。

- 設定画面のスライダーで調整する
- BGMとSEを別々に調整する
- 保存処理を入れて次回起動時にも反映する

お好みの設定に変えちゃってください。

### 新しい音を追加する手順（まとめ）

1. `SoundManager.cs` 内の `BGMData.BGMTYPE`（BGMの場合）または `SEData.SETYPE`（SEの場合）のenumに、新しいラベルを**末尾に追加**する
   - 既存のラベルの順番は変えない（他の人の作業とコンフリクトする・番号がズレるため）
2. Unityの `SoundManager` コンポーネントのInspectorで、`Bgm Sound Datas` または `Se Sound Datas` のリストに要素を追加し、追加したenumの種類と実際の `AudioClip`、音量を設定する
3. コードから `PlayBGM` / `PlaySE` に手順1で追加したenumを渡して呼び出す

enumを追加・変更した際は、SoundManagerコンポーネントのInspector側の設定もセットで更新すること（コード側だけ直してAudioClipの割り当てを忘れないよう注意）。

### （興味がある人向け）シングルトンってなに？

サウンドマネージャーを使うだけなら読まなくてOKです。コード自体に興味がある人向けの解説です。

SoundManagerはシングルトンで作成しています。理由は以下の3つです。

1. **別のシーンでも再生可能**: タイトルシーンにだけ配置していますが、`DontDestroyOnLoad` を使っているためシーン遷移してもオブジェクトが破棄されず、次のシーンにも残り続けます。シングルトンにしていることで、その残り続けているSoundManagerをどのスクリプトからでも同じ方法で参照できます。
2. **いろんなスクリプトから自由に参照できる**: 通常、他のスクリプトの関数を実行するときは `[SerializeField] SoundManager soundManager;` のように参照を変数で持つ必要があり、音を流したいスクリプトすべてにアタッチする必要があります。シングルトンなら共通の変数を使って取得できるため、どのスクリプトからでもSoundManagerのメソッドを実行できます。
3. **サウンド関係でやりたいことを一括で管理できる**: 音量の増減などの関数もシングルトンで用意することで利便性が上がります。

**シングルトンとは**: プログラミングでよく使われる考え方の一つで、「ゲーム内でSoundManagerは1つだけ存在するという条件をつけることで、どこからでも同じSoundManagerを呼び出せる仕組み」です。

コード上でやっていることは以下の3つです。

- 全体に向けて宣言
  ```csharp
  public static SoundManager Instance { get; private set; }
  ```
  SoundManager自身が「自分はゲーム内で使う唯一のSoundManagerです」という情報を`static`な変数`Instance`に保存しています。`static`変数なので、どのスクリプトからでも共通のSoundManagerを取得できます。

- 宣言した変数に登録
  ```csharp
  private void Awake()
  {
      if (Instance == null)
      {
          Instance = this;
          DontDestroyOnLoad(gameObject);
      }
  }
  ```
  `Start`よりも早い`Awake`のタイミングで、`Instance`に`this`を代入して登録します。`DontDestroyOnLoad`によって、シーン遷移後もオブジェクトが残り続けるようにしています。

- ゲーム内に1つだけ存在させる
  ```csharp
  private void Awake()
  {
      if (Instance == null)
      {
          Instance = this;
          DontDestroyOnLoad(gameObject);
      }
      else
      {
          Destroy(gameObject);
      }
  }
  ```
  `Instance`に何も登録されていなければ自身を登録し、すでに何か登録されていれば自身を削除します。シングルトンがゲーム内に2つ以上存在すると、どちらを参照しているのかわからなくなってしまうため、この対応が必要です。

以上のようにして、自身をゲーム全体に対して「SoundManagerである」と公開しており、他のスクリプトは`Instance`を使って具体的な参照を持たずにSoundManager内のメソッドを実行できます。
