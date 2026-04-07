# Unity Auto Rendering

複数のレンダリングPCに対して、Unityシーンの自動レンダリングをリモートで指示・管理するシステムです。

## インストール

### インストーラ（Service / Agent）

[**AutoRenderingSetup.exe をダウンロード**](https://github.com/MudShipProject/UnityAutoRendering/releases/latest/download/AutoRenderingSetup.exe) して実行します。

| タイプ | 内容 | 用途 |
|---|---|---|
| **Full** | Agent + Agent Config + Service | 1台で全て使う場合 |
| **Agent only** | Agent + Agent Config | レンダリングPC |
| **Service only** | Service | 操作PC |
| **Custom** | 個別選択 | 必要なものだけ |

#### オプション
- **デスクトップショートカット作成**
- **Windows起動時にAgentを自動起動**

#### アンインストール
- **スタートメニュー** > 「Auto Rendering System」>「Uninstall」
- **Windowsの設定** > 「アプリ」>「インストールされているアプリ」> 「Auto Rendering System」

### Unityパッケージ（UPM）

Unity の Package Manager から Git URL でインストールできます。

1. Unity で **Window > Package Manager** を開く
2. 左上の **+** > **Add package from git URL...** を選択
3. 以下のURLを入力:

```
https://github.com/MudShipProject/UnityAutoRendering.git?path=Assets/AutoRendering
```

または `Packages/manifest.json` に直接追加:

```json
{
  "dependencies": {
    "com.mudship.autorendering": "https://github.com/MudShipProject/UnityAutoRendering.git?path=Assets/AutoRendering"
  }
}
```

## セットアップ

### 1. 描画PC（Agent側）

1. インストーラで **Agent only** を選択してインストール
2. **Agent Config** を開き、以下を設定して Save

| 項目 | 説明 | 例 |
|---|---|---|
| IP Address | Agentのリッスンアドレス | `0.0.0.0`（全インターフェース） |
| Port | リッスンポート | `9000` |
| Unity Project Path | Unityプロジェクトのルート（Assetsの親） | `D:\MyProject` |
| Take | 録画テイク番号（レンダリング毎に自動加算） | `1` |

3. Agent を起動（バックグラウンドで常駐）

### 2. 操作PC（Service側）

1. インストーラで **Service only** を選択してインストール
2. **AutoRendering Service** を起動
3. 「Add」で描画PCのIPとポートを登録
4. 「Refresh」でAgentからシーン一覧を取得
5. レンダリングしたいシーンにチェック

### 3. Unityプロジェクト（描画PC）

1. UPM で Auto Rendering パッケージをインストール（[手順はこちら](#unityパッケージupm)）
2. **必須パッケージ**（依存関係として自動インストールされます）:
   - `com.unity.recorder` (4.0.3+)
   - `com.unity.timeline` (1.7.7+)
3. レンダリング対象シーンに以下をセットアップ:
   - 空のゲームオブジェクトを作成し、`AutoRendering` コンポーネントを追加
   - `RenderJobSettings` ScriptableObject を作成（Create > Auto Rendering > Render Job Settings）
   - Recorder の種類（Movie / Image Sequence / Animation / Audio）と出力設定を構成
   - `AutoRendering` コンポーネントに `RenderJobSettings` と `PlayableDirector` を割り当て

## 使い方

### レンダリング開始

1. Service でシーンにチェックを入れる
2. **▶ Start Rendering** をクリック
3. ボタンが赤い **⏹ Recording** に変わり、ログにシーン名が表示される
4. 全シーン完了後、自動的に **▶ Start Rendering** に戻る

### レンダリング中断

以下のいずれかの方法で中断できます：

- **Service**: 赤い「⏹ Recording」ボタンをクリック
- **Agent Config**: 「Stop Rendering」ボタンをクリック（描画PC上で直接）

### エンドポイント管理

| 操作 | 方法 |
|---|---|
| 追加 | 「Add」ボタン |
| 編集 | エンドポイントノードをダブルクリック |
| 削除 | 選択して「Remove」ボタン |
| シーン更新 | 「Refresh」ボタン |

## 設定ファイル

全ての設定は `%LOCALAPPDATA%` 以下に保存されます。

| ファイル | パス |
|---|---|
| Agent設定 | `%LOCALAPPDATA%\AutoRenderingAgent\agent-config.json` |
| Agentログ | `%LOCALAPPDATA%\AutoRenderingAgent\logs\YYYY-MM-DD.log` |
| Service設定 | `%LOCALAPPDATA%\AutoRenderingService\config.json` |

## OSCプロトコル

### Service → Agent

| アドレス | 引数 | 説明 |
|---|---|---|
| `/scenes/list` | なし | シーン一覧を要求 |
| `/render/start` | シーン名 (string...) | レンダリング開始 |
| `/render/stop` | なし | 現在のレンダリングを中断 |

### Agent → Service

| アドレス | 引数 | 説明 |
|---|---|---|
| `/scenes/result` | シーン名 (string...) | シーン一覧の応答 |
| `/render/started` | シーン名 (string) | シーンのレンダリング開始通知 |
| `/render/stopped` | シーン名 (string) | シーンのレンダリング失敗/中断通知 |
| `/render/finished` | なし | 全シーンのレンダリング完了通知 |

## 技術スタック

- **.NET 8.0** (Windows Forms)
- **CoreOSC 1.0.0** — OSC通信
- **Unity Recorder 4.0.3+** — 録画
- **Unity Timeline 1.7.7+** — Timeline再生
- **Inno Setup 6** — インストーラ作成

## ライセンス

MIT License
