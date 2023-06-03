using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


//EditorDefaultResources폴더 설명
//이 폴더는 Resources 폴더와 유사하지만 에디터 스크립트들 에서만 의미를 가진다. 만약 에디터 플러그 인에
//에셋들(예를 들어 아이콘, GUI 스킨 등)을 로드해야 하지만 빌드에는 포함되어야 하지 않다면 이 폴더를 사용해라
//(이러한 파일들은 그냥 Resources 폴더에 넣는다면 빌드에도 포함된다는 것을 의미한다).

[CustomEditor(typeof(LevelCreator))]
[ExecuteInEditMode]
public class LevelCreatorInspector : Editor
{
    Dictionary<NodeTypes, Texture> textureHolder = new Dictionary<NodeTypes, Texture>();

    private void OnEnable()
    {
        textureHolder.Add(NodeTypes.Empty, (Texture)EditorGUIUtility.Load("Assets/EditorDefaultResources/Empty.png"));
        textureHolder.Add(NodeTypes.Process, (Texture)EditorGUIUtility.Load("Assets/EditorDefaultResources/Process.png"));
        textureHolder.Add(NodeTypes.Resource, (Texture)EditorGUIUtility.Load("Assets/EditorDefaultResources/Resource.png"));
    }
    NodeTypes currentSelected = NodeTypes.Empty;
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();
        GUILayout.Label("CurrentSelected : " + currentSelected.ToString());

        LevelCreator levelCreator = (LevelCreator)target;

        levelCreator.SetEmptyLevels();

        GUILayout.BeginVertical();
        for(int r = levelCreator.row - 1; r >= 0; r--){
            GUILayout.BeginHorizontal();
            for(int c = 0; c < levelCreator.col; c++){
                if(GUILayout.Button(textureHolder[levelCreator.level[c+((levelCreator.col)*r)].nodeTypes], GUILayout.Width(50), GUILayout.Height(50))){
                    levelCreator.level[c+((levelCreator.col)*r)].nodeTypes = currentSelected;
                }
                GUILayout.Label((c + ((levelCreator.col)*r)).ToString(), GUILayout.Width(20), GUILayout.Height(15));
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        GUILayout.Space(20);
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        int count = 0;
        foreach(KeyValuePair<NodeTypes, Texture> e in textureHolder){
            count++;
            if(GUILayout.Button(e.Value, GUILayout.Width(50), GUILayout.Height(50))){
                currentSelected = e.Key;
            }
            if(count % 5 == 0){
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
}
