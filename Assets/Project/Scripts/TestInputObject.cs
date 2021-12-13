using UnityEngine;
using Com.Bit34Games.Unity.Input;

public class TestInputObject : InputObjectComponent
{
    //  MEMBERS
#pragma warning disable 0649
    //      For Editor
    [SerializeField] private bool _IsSprite;
    [SerializeField] private Material _disabledMaterial;
    [SerializeField] private Material _highlightMaterial;
#pragma warning restore 0649
    //      Internal
    static private int _Id;
    private Material _material;


    //  METHODS
    void Start()
    {
        int category = (int)(_IsSprite?TestInputObjectCategory.Cat2D:TestInputObjectCategory.Cat3D);
        int id       = ++_Id;
        _material    = gameObject.GetComponent<Renderer>().material;

        Initialize(category, id);
        SetState(true);
    }

    protected override void InputStateChanged()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.material = (IsInputEnabled) ? (_material) : (_disabledMaterial);
    }

    public void SetHighlight(bool state)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.material = (state) ? (_highlightMaterial) : (_material);
    }
}
