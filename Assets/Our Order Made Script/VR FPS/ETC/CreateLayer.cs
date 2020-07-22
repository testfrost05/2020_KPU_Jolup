using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CreateLayerCollisionMatrix))]
public class LayerMaskEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Set Layer Collision Matrix"))
        {
            LoadVArmoryLayers();
            SetVArmoryLayerCollision();
        }
    }

        /// <summary>
        /// Create a layer at the next available index. Returns silently if layer already exists.
        /// </summary>
        /// <param name="name">Name of the layer to create</param>
    public static void CreateLayer(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new System.ArgumentNullException("name", "New layer name string is either null or empty.");

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layerProps = tagManager.FindProperty("layers");
        var propCount = layerProps.arraySize;

        SerializedProperty firstEmptyProp = null;

        for (var i = 0; i < propCount; i++)
        {
            var layerProp = layerProps.GetArrayElementAtIndex(i);

            var stringValue = layerProp.stringValue;

            if (stringValue == name) return;

            if (i < 8 || stringValue != string.Empty) continue;

            if (firstEmptyProp == null)
                firstEmptyProp = layerProp;
        }

        if (firstEmptyProp == null)
        {
            UnityEngine.Debug.LogError("Maximum limit of " + propCount + " layers exceeded. Layer \"" + name + "\" not created.");
            return;
        }

        firstEmptyProp.stringValue = name;
        tagManager.ApplyModifiedProperties();
    }

    public static void LoadVArmoryLayers()
    {
        CreateLayer("Player");
        CreateLayer("ItemDetection");
        CreateLayer("Item");
        CreateLayer("HeldItem");
        CreateLayer("InventoryItem");
        CreateLayer("Door");
        CreateLayer("Projectile");
        CreateLayer("Ragdoll");
        CreateLayer("Ground");
        CreateLayer("Character");
    }

    public static void SetVArmoryLayerCollision()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Door"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("InventoryItem"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("HeldItem"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Item"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("ItemDetection"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Player"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Projectile"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Ground"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Door"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("InventoryItem"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("HeldItem"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Item"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("ItemDetection"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Player"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Projectile"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ground"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("Door"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("InventoryItem"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("HeldItem"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("Item"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("ItemDetection"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("Projectile"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("Ground"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("ItemDetection"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("Door"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("InventoryItem"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("HeldItem"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("Item"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("Projectile"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("Ground"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Item"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HeldItem"), LayerMask.NameToLayer("Door"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HeldItem"), LayerMask.NameToLayer("InventoryItem"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HeldItem"), LayerMask.NameToLayer("HeldItem"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HeldItem"), LayerMask.NameToLayer("Projectile"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HeldItem"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HeldItem"), LayerMask.NameToLayer("Ground"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HeldItem"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("InventoryItem"), LayerMask.NameToLayer("Door"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("InventoryItem"), LayerMask.NameToLayer("InventoryItem"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("InventoryItem"), LayerMask.NameToLayer("Projectile"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("InventoryItem"), LayerMask.NameToLayer("Ragdoll"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("InventoryItem"), LayerMask.NameToLayer("Ground"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("InventoryItem"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Door"), LayerMask.NameToLayer("Door"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Door"), LayerMask.NameToLayer("Projectile"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Door"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Door"), LayerMask.NameToLayer("Ground"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Door"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Projectile"), LayerMask.NameToLayer("Projectile"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Projectile"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Projectile"), LayerMask.NameToLayer("Ground"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Projectile"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ragdoll"), LayerMask.NameToLayer("Ragdoll"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ragdoll"), LayerMask.NameToLayer("Ground"), false);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ragdoll"), LayerMask.NameToLayer("Character"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ground"), LayerMask.NameToLayer("Ground"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ground"), LayerMask.NameToLayer("Character"), false);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Character"), LayerMask.NameToLayer("Character"), true);
    }
}
