using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class ProjectViewTabList : EditorWindow
{
    #region ### Parameters ###

    #region ### Classes ###
    
    [System.Serializable]
    class AssetInfo
    {
        public string guid;
        public string path;
        public string name;
        public string type;
    }

    [System.Serializable]
    class AssetInfoList
    {
        public List<AssetInfo> infoList = new List<AssetInfo>();
    }


    #endregion ### Classes ###

    [Header("Cache")]
    static AssetInfo lastOpenedAsset = null;
    static string strNowPath;
    static BindingFlags bindingFlags;
    static System.Type typeProjectBrowser;
    static EditorWindow projectBrowserWindow;

    [Header("Layout")]
    float shortcutListCmdHeight = 30;
    float plusbuttonHeight = 20;
    /// <summary>
    /// リストに表示する最大文字数
    /// </summary>
    const int NumOfCharactorsVisible = 15;

    [Header("GUI")]
    Vector2 scrollView;

    [Header("Icon")]
    static Texture texIconHome;
    static Texture texIconActive;
    static Texture texIconCopy;
    static Texture texIconOption;

    [Header("Assets")]
    [SerializeField] static AssetInfoList assetsCache = null;
    static AssetInfoList assets
    {
        get
        {
            if (assetsCache == null)
            {
                assetsCache = new AssetInfoList();
                InitializeAssets();
            }

            return assetsCache;
        }
    }

    [Header("Flag")]
    static bool isSynced = false;
    static bool isDebug = false;
    static bool isShowLastOpenedAsset = false;

    #endregion ### Parameters ###

    #region ### Methods ###

    [MenuItem("Tools/ProjectView/Tabbar")]
    static void OpenWindow()
    {
        GetWindow<ProjectViewTabList>("P.Tab");

    }

    [InitializeOnLoadMethod]
    static void InitializeOnLoad()
    {
        texIconHome = Resources.Load<Texture>("icon_home");
        texIconActive = Resources.Load<Texture>("icon_active");
        texIconCopy = Resources.Load<Texture>("icon_copy");
        texIconOption = Resources.Load<Texture>("icon_option");

        /*
        // プロジェクトビューの情報を取得
        bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var asm = Assembly.Load("UnityEditor.dll");
        typeProjectBrowser = asm.GetType("UnityEditor.ProjectBrowser");
        projectBrowserWindow = GetWindow(typeProjectBrowser);
        */
    }

    void Update()
    {
        
        strNowPath = GetCurrentDirectory();
        
        if (strNowPath.Equals("") || lastOpenedAsset == null)
        {
            return;
        }

        if (strNowPath != lastOpenedAsset.path)
        {
            ChangeBookmarkAsset();
            Repaint();
        }

    }

    /// <summary>
    /// Projectビューの現在の作業ディレクトリを取得する
    /// </summary>
    /// <returns></returns>
    static string GetCurrentDirectory()
    {
        
        bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var asm = Assembly.Load("UnityEditor.dll");
        typeProjectBrowser = asm.GetType("UnityEditor.ProjectBrowser");
        
        projectBrowserWindow = GetWindow(typeProjectBrowser);
        
        return (string)typeProjectBrowser.GetMethod("GetActiveFolderPath", bindingFlags).Invoke(projectBrowserWindow, null);
    }

    #region ### assets ###

    static void InitializeAssets()
    {
        var info = new AssetInfo();
        info.path = "Assets";
        info.guid = AssetDatabase.AssetPathToGUID(info.path);
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(info.path);
        info.name = asset.name;
        info.type = asset.GetType().ToString();
        assets.infoList.Add(info);

        lastOpenedAsset = info;
    }

    void AddAssetsTab()
    {
        var info = new AssetInfo();
        info.path = "Assets";
        info.guid = AssetDatabase.AssetPathToGUID(info.path);
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(info.path);
        info.name = asset.name;
        info.type = asset.GetType().ToString();
        assets.infoList.Add(info);

        lastOpenedAsset = info;
    }

    void BookmarkAsset()
    {
        foreach (string assetGuid in Selection.assetGUIDs)
        {
            // 重複を許可しないときはコメントを外す
            /*
            if (assets.infoList.Exists(x => x.guid == assetGuid))
            {
                continue;
            }
            */

            var info = new AssetInfo();
            info.guid = assetGuid;
            info.path = AssetDatabase.GUIDToAssetPath(assetGuid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(info.path);
            info.name = asset.name;
            info.type = asset.GetType().ToString();

            // ファイルは登録できない
            if (!File.GetAttributes(info.path).HasFlag(FileAttributes.Directory))
            {
                return;
            }

            assets.infoList.Add(info);
        }
    }

    void RemoveAsset(AssetInfo info)
    {
        assets.infoList.Remove(info);
        
        // 現在選択中のタブを削除した場合
        if(info == lastOpenedAsset)
        {
            // 一番最後尾のディレクトリを選択する
            lastOpenedAsset = assets.infoList[assets.infoList.Count - 1];
        }
    }

    void ChangeBookmarkAsset()
    {
        
        var info = new AssetInfo();
        
        info.path = strNowPath;
        info.guid = AssetDatabase.AssetPathToGUID(strNowPath);
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(info.path);
        info.name = asset.name;
        info.type = asset.GetType().ToString();

        // ファイルは登録できない
        if (!File.GetAttributes(info.path).HasFlag(FileAttributes.Directory))
        {
            return;
        }

        for (int i = 0; i < assets.infoList.Count; i++)
        {
            if(assets.infoList[i] == lastOpenedAsset)
            {
                assets.infoList[i] = info;
                break;
            }
        }
        lastOpenedAsset = info;
    }

    void OpenAsset(AssetInfo info)
    {

        // 最後に開いたアセットを記録する
        lastOpenedAsset = info;

        // シーンアセット以外のアセットの処理
        var asset = AssetDatabase.LoadAssetAtPath<Object>(info.path);
        AssetDatabase.OpenAsset(asset);
    }

    void BackHome()
    {
        // シーンアセット以外のアセットの処理
        var asset = AssetDatabase.LoadAssetAtPath<Object>("Assets");
        AssetDatabase.OpenAsset(asset);
    }

    #endregion ### assets ###

    #region ### Draw GUI ###

    void OnGUI()
    {
        // ヘッダー
        GUILayout.BeginHorizontal();
        {
            // Assetsに戻る
            var content = new GUIContent(texIconHome, "Assetsに戻る");
            if (GUILayout.Button(content, GUILayout.Width(20), GUILayout.Height(20)))
            {
                BackHome();
            }

            GUILayout.FlexibleSpace();
            /*
            // タブ追加（Assets）
            content = new GUIContent("+", "タブを追加");
            if (GUILayout.Button(content, GUILayout.Width(20), GUILayout.Height(20)))
            {
                AddAssetsTab();
            }
            */
            /*
            // タブ追加（現在ProjectViewで選択しているものを生成）
            content = new GUIContent(texIconCopy, "タブを複製");
            if (GUILayout.Button(content, GUILayout.Width(20), GUILayout.Height(20)))
            {
                BookmarkAsset();
            }
            */

            // オプション
            content = new GUIContent(texIconOption, "設定を開く");
            if (GUILayout.Button(content, GUILayout.Width(20), GUILayout.Height(20)))
            {
                isDebug = !isDebug;
            }
        }
        GUILayout.EndHorizontal();

        // 仕切り線
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

        // メインコンテンツの表示
        scrollView = GUILayout.BeginScrollView(scrollView);
        {
            if (isDebug)   // デバッグ
            {
                DrawSettingMenu();
            }
            else   // リストの表示
            {
                
                foreach (var info in assets.infoList)
                {
                    GUILayout.BeginHorizontal();
                    {
                        bool isCanceled = DrawAssetRow(info);
                        if (isCanceled)
                        {
                            break;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                // タブ追加（Assets）
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(13);

                    // タブ追加ボタン
                    var content = new GUIContent("+", "タブを追加");
                    if (GUILayout.Button(content, GUILayout.ExpandWidth(true), GUILayout.Height(plusbuttonHeight)))
                    {
                        AddAssetsTab();
                    }
                }
                GUILayout.EndHorizontal();
            }
            

        }
        GUILayout.EndScrollView();


        // フッター
        GUILayout.FlexibleSpace();

        // 仕切り線
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

        GUILayout.BeginHorizontal();
        {
            if (isShowLastOpenedAsset)
            {
                GUILayout.Label($"Last: {lastOpenedAsset.path}");
            }
            
            GUILayout.FlexibleSpace();
            
            /*
            // オプション
            var content = new GUIContent(texIconOption, "設定を開く");
            if (GUILayout.Button(content, GUILayout.Width(20), GUILayout.Height(20)))
            {
                isDebug = !isDebug;
            }
            */
        }
        GUILayout.EndHorizontal();

        // ▼デバッグ用
        //GUILayout.Label($"last:{lastOpenedAsset.name}");
        //GUILayout.Label($"tex:{texIconHome!=null}");
    }

    bool DrawAssetRow(AssetInfo info)
    {
        bool isCanceled = false;

        // アセットのボタンを表示する
        {
            DrawAssetItemButton(info);
        }

        // アセットをリストから削除するボタンを表示
        var content = new GUIContent("×", "タブから削除");
        if (GUILayout.Button(content, GUILayout.ExpandWidth(false), GUILayout.Height(shortcutListCmdHeight)))
        {
            RemoveAsset(info);
            isCanceled = true;
        }

        return isCanceled;
    }

    void DrawAssetItemButton(AssetInfo info)
    {
        string infoName = (info.name.Length > NumOfCharactorsVisible) ? $"{info.name.Substring(0, NumOfCharactorsVisible - 3)}..." : info.name;

        var content = new GUIContent($"{infoName}");
        var style = GUI.skin.button;
        var originalAlignment = style.alignment;
        var originalFontStyle = style.fontStyle;
        var originalTextColor = style.normal.textColor;
        var originalFontSize = style.fontSize;
        style.alignment = TextAnchor.MiddleLeft;
        style.fontSize = 12;


        if (info == lastOpenedAsset)
        {
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
        }
        else
        {
            style.normal.textColor = Color.gray;
        }
        
        // アクティブかどうか判別するためのボックス
        Color clrContent = GUI.color;
        GUI.color = (info == lastOpenedAsset) ? Color.cyan : Color.gray;
        GUILayout.Label(texIconActive, GUILayout.Width(5), GUILayout.Height(shortcutListCmdHeight));
        
        GUI.color = clrContent;
        

        float width = position.width - 30f;
        if (GUILayout.Button(content, style, GUILayout.MaxWidth(width), GUILayout.Height(shortcutListCmdHeight)))
        {
            OpenAsset(info);
        }

        style.alignment = originalAlignment;
        style.fontStyle = originalFontStyle;
        style.normal.textColor = originalTextColor;
        style.fontSize = originalFontSize;
    }

    void DrawSettingMenu()
    {
        GUILayout.Label("Settings");
        GUILayout.Label($"最大表示文字数: {NumOfCharactorsVisible,2}");
        GUILayout.Label($"リストの高さ: {shortcutListCmdHeight,2}");


        GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));


        GUILayout.Label("Debug Mode");

        GUILayout.Label($"Sync: {(isSynced ? "ON" : "off")}");
        
        GUILayout.Label($"現在のディレクトリ：{GetCurrentDirectory()}");

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("最後に選択したディレクトリを表示");
            if (GUILayout.Button($"{(isShowLastOpenedAsset ? "ON" : "off")}"))
            {
                isShowLastOpenedAsset = !isShowLastOpenedAsset;
            }
        }
        GUILayout.EndHorizontal();
        
    }

    #endregion ### Draw GUI ###


    #endregion ### Methods ###
}
