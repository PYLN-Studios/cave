using UnityEditor;

public static class CreateDOTSTemplates {

    [MenuItem("Assets/Create/DOTS Templates/ISystem", priority=40)]
    public static void CreateISystem() {
        string templatePath = "Assets/Editor/ISystem.cs.txt";

        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "System.cs");
    }

    [MenuItem("Assets/Create/DOTS Templates/Component", priority = 40)]
    public static void CreateComponent() {
        string templatePath = "Assets/Editor/Component.cs.txt";

        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "Component.cs");
    }

    [MenuItem("Assets/Create/DOTS Templates/Authoring", priority = 40)]
    public static void CreateAuthoring() {
        string templatePath = "Assets/Editor/Authoring.cs.txt";

        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "Authoring.cs");
    }
}
