using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// UWAGA: ten plik MUSI leżeć w folderze o nazwie "Editor"
// (np. Assets/Editor/SpriteRendererToImageConverter.cs), inaczej się nie skompiluje.
public static class SpriteRendererToImageConverter
{
    [MenuItem("Tools/UI/Convert SpriteRenderers \u2192 Images (Selection)")]
    static void ConvertSelection()
    {
        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("Nic nie zaznaczone. Zaznacz Canvas albo obiekt rodzica i spr\u00f3buj ponownie.");
            return;
        }

        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Convert SpriteRenderers to Images");

        int count = 0;
        foreach (var root in roots)
        {
            // true = uwzgl\u0119dnij te\u017c nieaktywne dzieci
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                ConvertOne(sr);
                count++;
            }
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"Podmieniono {count} SpriteRenderer \u2192 Image.");
    }

    static void ConvertOne(SpriteRenderer sr)
    {
        var go = sr.gameObject;

        // zapami\u0119taj dane zanim usuniesz komponent
        Sprite  sprite     = sr.sprite;
        Color   color      = sr.color;
        bool    flipX      = sr.flipX;
        bool    flipY      = sr.flipY;
        Vector3 worldScale = sr.transform.localScale;   // skala u\u017cyta do wymiarowania w \u015bwiecie

        // usu\u0144 SpriteRenderer
        Undo.DestroyObjectImmediate(sr);

        // dodaj Image \u2014 to automatycznie zamienia Transform na RectTransform
        // i dok\u0142ada CanvasRenderer (bo Graphic ma [RequireComponent])
        var image = Undo.AddComponent<Image>(go);
        image.sprite = sprite;
        image.color  = color;

        var rt = image.rectTransform;

        // Rozmiar w JEDNOSTKACH \u015aWIATA (sprite.bounds = rect / PixelsPerUnit),
        // domno\u017cony przez dotychczasow\u0105 skal\u0119 obiektu. Nie mno\u017cymy przez PPU/100,
        // bo Canvas w trybie World Space u\u017cywa jednostek \u015bwiata, nie pikseli.
        if (sprite != null)
        {
            Vector2 world = sprite.bounds.size;
            rt.sizeDelta = new Vector2(
                world.x * Mathf.Abs(worldScale.x),
                world.y * Mathf.Abs(worldScale.y));
        }

        // Skala wraca do 1. Znak zachowujemy, \u017ceby odwzorowa\u0107 odbicia
        // (flipX/flipY ze SpriteRenderera albo ujemn\u0105 skal\u0119 z transformu).
        float signX = Mathf.Sign(worldScale.x == 0f ? 1f : worldScale.x) * (flipX ? -1f : 1f);
        float signY = Mathf.Sign(worldScale.y == 0f ? 1f : worldScale.y) * (flipY ? -1f : 1f);
        rt.localScale = new Vector3(signX, signY, 1f);

        EditorUtility.SetDirty(go);
    }
}