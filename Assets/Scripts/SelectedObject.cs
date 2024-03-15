using UnityEngine;

public class SelectedObject : Outline
{
    public bool Selected {
        get { return selected; }
        set {
            selected = value;
            OutlineColor = value ? Color.yellow : Color.white;
        }
    }
    private bool selected;

    public SelectedObject() {
        OutlineMode = Mode.OutlineAll;
        OutlineWidth = 10f;
        selected = false;
    }
}
