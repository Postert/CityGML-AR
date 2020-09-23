using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Creates GameObjects from <c>Annotation</c> objects and manage its static and dynamic appearance.
/// </summary>
public class SimpleTextAnnotationUnityTemplate : MonoBehaviour
{
    /// <value>Specifies whether the <c>Annotation</c> is dynamically orientated to the user or has a static orientation.</value>
    public bool IsLookingToARCamera { get; set; } = false;

    /// <value>Specifies whether the <c>Annotation</c> size increases/decreases when the user moves away from / comes closer to it. By scaling depended on the user's distance, the <c>Annotation</c> size keeps the same while its size changes dynamically in the Unity3D scene. By not scaling with the user's distance, the <c>Annotation</c> has a static size</value>
    public bool IsScalingWithARCameraDistance { get; set; } = false;

    private Canvas AnnotationCanvas;
    private Text TextComponent;
    private GameObject TextField;
    private CanvasScaler AnnotationCanvasScaler;

    private const int FontSize = 25;
    private RectTransform RectTransform;
    private Vector3 RectTransformScaleOneMeterDistance;

    /// <summary>
    /// Sets the <c>Annotation</c> text and updates name of its GameObject.
    /// </summary>
    /// <param name="annotationText">Text to be displayed in the <c>Annotation</c></param>
    public void SetAnnotationText(string annotationText)
    {
        TextComponent.text = annotationText;
        name = annotationText;
    }

    /// <summary>
    /// Set the initial size of the <c>Annotation</c>.
    /// </summary>
    /// <param name="localScale">Size</param>
    public void SetLocalScale(float localScale)
    {
        RectTransform.localScale = new Vector3(localScale, localScale, localScale);
    }



    /// <summary>
    /// Creates a textfield in the Unity3D scene.
    /// </summary>
    void Awake()
    {
        RectTransformScaleOneMeterDistance = new Vector3(0.01f, 0.01f, 1f);

        // Initialize Annotation with Canvas
        AnnotationCanvas = gameObject.AddComponent<Canvas>();
        AnnotationCanvas.renderMode = RenderMode.WorldSpace;
        AnnotationCanvasScaler = gameObject.AddComponent<CanvasScaler>();
        AnnotationCanvasScaler.scaleFactor = 10.0f;
        AnnotationCanvasScaler.dynamicPixelsPerUnit = 50f;
        gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 3.0f);
        gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 3.0f);


        gameObject.name = "Annotation";
        bool bWorldPosition = false;

        gameObject.GetComponent<RectTransform>().SetParent(transform, bWorldPosition);
        gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
        gameObject.transform.localScale = new Vector3(1, 1, 1);

        // Initialize Canvas with text component
        TextField = new GameObject();
        TextField.name = "Text";
        TextField.transform.parent = gameObject.transform;
        TextComponent = TextField.AddComponent<Text>();

        RectTransform = TextField.GetComponent<RectTransform>();
        RectTransform.localScale = RectTransformScaleOneMeterDistance;
        RectTransform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 3.0f);
        RectTransform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 3.0f);

        TextComponent.alignment = TextAnchor.MiddleCenter;
        TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        TextComponent.verticalOverflow = VerticalWrapMode.Overflow;
        Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        TextComponent.font = ArialFont;
        TextComponent.fontSize = FontSize;
        TextComponent.text = "";
        TextComponent.enabled = true;
        TextComponent.color = Color.white;
    }

    /// <summary>
    /// Rescales and reorienteates the <c>Annotation</c> in case is it specified with the <c>IsLookingToARCamera</c> and <c>IsScalingWithARCameraDistance</c> property.
    /// </summary>
    void Update()
    {
        Vector3 cameraPosition = Camera.main.transform.position;

        if (IsLookingToARCamera)
        {
            transform.LookAt(cameraPosition);

            /// Rotate so the visible side faces the camera.
            transform.Rotate(0, 180, 0);
        }

        if (IsScalingWithARCameraDistance)
        {
            /// Scaling annotation to keep the same size by calculating the scale depending on the distance of the AR camera to the annotation
            float annotationCameraDistance = Vector3.Distance(cameraPosition, gameObject.transform.position);
            RectTransform.localScale = RectTransformScaleOneMeterDistance * annotationCameraDistance;
        }
    }
}