using UnityEngine;

[CreateAssetMenu(fileName = "LevelInfo", menuName = "QuiverWorld/Level/LevelInfo")]
public class LevelInfo : ScriptableObject
{
    public enum LevelType
    {
        Generic,
        Gameplay,
        Menu
    }

    public Object mainScene;

    [Tooltip("The leveltype determines e.g. what happens when you hit play in editor")]
    public LevelType levelType = LevelType.Gameplay;

    [Tooltip("Should the level be included in the build")]
    public bool includeInBuild = true;
}