using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectReadme", menuName = "CollieLab/Readme", order = 1)]
public class ProjectReadme : ScriptableObject
{
    #region Serialized Field
    [SerializeField] private Texture2D icon = null;
    public Texture2D Icon => icon;

    [SerializeField] private string title = null;
    public string Title => title ?? "NULL";

    [SerializeField] private Section[] sections = null;
    public Section[] Sections => sections;

    [SerializeField] private bool loadedLayout = false;
    public bool LoadedLayout { get => loadedLayout; set => loadedLayout = value; }

    [SerializeField] private Font font = null;
    public Font Font => font;
    #endregion

    [Serializable]
    public class Section
    {
        public string heading = null;
        public string text = null;
        public string linkText = null;
        public string url = null;
    }
}