using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu]
public class BTTree : ScriptableObject
{
    public BTRootNode rootNode;
    public BTNode.State state = BTNode.State.Running;
    public List<BTNode> nodes = new();
    public BTBlackboard blackboard;
    public BTRuntime runtime;

    public BTNode.State Update()
    {
        if (rootNode.state == BTNode.State.Running) state = rootNode.Update();
        return state;
    }

#if UNITY_EDITOR
    public BTBlackboard CreateBlackboard()
    {
        BTBlackboard blackboard = CreateInstance<BTBlackboard>();
        blackboard.name = "Blackboard";
        AssetDatabase.AddObjectToAsset(blackboard, this);
        AssetDatabase.SaveAssets();
        return blackboard;
    }

    public BTNode CreateNode(System.Type type)
    {
        BTNode node = CreateInstance(type) as BTNode;
        node.name = type.Name;
        node.guid = GUID.Generate().ToString();
        node.tree = this;

        Undo.RecordObject(this, "Behaviour Tree (Create Node)");
        nodes.Add(node);
        if (!Application.isPlaying)
        {
            AssetDatabase.AddObjectToAsset(node, this);
        }
        Undo.RegisterCreatedObjectUndo(node, "Behaviour Tree (Create Node)");
        AssetDatabase.SaveAssets();
        return node;
    }

    public void RemoveNode(BTNode node)
    {
        Undo.RecordObject(this, "Behaviour Tree (Delete Node)");
        nodes.Remove(node);
        //AssetDatabase.RemoveObjectFromAsset(node);
        Undo.DestroyObjectImmediate(node);
        AssetDatabase.SaveAssets();
    }

    public void AddChild(BTNode parent, BTNode child)
    {
        BTDecoratorNode decorator = parent as BTDecoratorNode;
        if (decorator)
        {
            Undo.RecordObject(decorator, "Behaviour Tree (Add Child)");
            decorator.child = child;
            EditorUtility.SetDirty(decorator);
        }

        BTCompositeNode composite = parent as BTCompositeNode;
        if (composite)
        {
            Undo.RecordObject(composite, "Behaviour Tree (Add Child)");
            composite.children?.Add(child);
            EditorUtility.SetDirty(composite);
        }

        BTRootNode root = parent as BTRootNode;
        if (root)
        {
            Undo.RecordObject(root, "Behaviour Tree (Add Child)");
            root.child = child;
            EditorUtility.SetDirty(root);
        }
        /*switch (parent)
        {
            case BTDecoratorNode:
                (parent as BTDecoratorNode).child = child;
                break;

            case BTCompositeNode:
                (parent as BTCompositeNode).children.Add(child);
                break;

            case BTRootNode:
                (parent as BTRootNode).child = child;
                break;
        }*/
    }

    public void RemoveChild(BTNode parent, BTNode child)
    {
        BTDecoratorNode decorator = parent as BTDecoratorNode;
        if (decorator)
        {
            Undo.RecordObject(decorator, "Behaviour Tree (Remove Child)");
            decorator.child = null;
            EditorUtility.SetDirty(decorator);
        }

        BTCompositeNode composite = parent as BTCompositeNode;
        if (composite)
        {
            Undo.RecordObject(composite, "Behaviour Tree (Remove Child)");
            composite.children?.Remove(child);
            EditorUtility.SetDirty(composite);
        }

        BTRootNode root = parent as BTRootNode;
        if (root)
        {
            Undo.RecordObject(root, "Behaviour Tree (Remove Child)");
            root.child = null;
            EditorUtility.SetDirty(root);
        }
        /*switch (parent)
        {
            case BTDecoratorNode:
                (parent as BTDecoratorNode).child = null;
                break;

            case BTCompositeNode:
                (parent as BTCompositeNode).children.Remove(child);
                break;

            case BTRootNode:
                (parent as BTRootNode).child = null;
                break;
        }*/
    }
#endif
    public List<BTNode> GetChildren(BTNode parent)
    {
        List<BTNode> list = new();
        switch (parent)
        {
            case BTDecoratorNode:
                if ((parent as BTDecoratorNode).child != null)
                    list.Add((parent as BTDecoratorNode).child);
                break;

            case BTRootNode:
                if ((parent as BTRootNode).child != null)
                    list.Add((parent as BTRootNode).child);
                break;

            case BTCompositeNode:
                return (parent as BTCompositeNode).children;
        }
        return list;
    }

    public void Traverse(BTNode node, System.Action<BTNode> visiter)
    {
        if (node)
        {
            visiter.Invoke(node);
            var children = GetChildren(node);
            children.ForEach((n) => Traverse(n, visiter));
        }
    }

    public BTTree Clone()
    {
        BTTree tree = Instantiate(this);
        tree.rootNode = tree.rootNode.Clone() as BTRootNode;
        tree.nodes = new List<BTNode>();
        tree.blackboard = blackboard.Clone();
        Traverse(tree.rootNode, (n) => { tree.nodes.Add(n); n.tree = tree; });    
        return tree;
    }
}
