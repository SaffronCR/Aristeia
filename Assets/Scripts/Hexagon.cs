using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hexagon : MonoBehaviour
{
    public int m_coordX = 0;
    public int m_coordY = 0;
    public float m_size = 1f;

    private HexType m_prevType = HexType.Empty;
    private Transform m_transform;
    private HexType type = HexType.Empty;

    public HexType PrevType
    {
        get
        {
            return m_prevType;
        }

        set
        {
            m_prevType = value;
        }
    }

    public HexType Type
    {
        get
        {
            return type;
        }

        set
        {
            type = value;
        }
    }

    public bool BlocksLoS()
    {
        return (Type == HexType.Wall || Type == HexType.Character);
    }

    public Vector2 CalcCenterToScenePos()
    {
        return new Vector2(m_transform.position.x, m_transform.position.z);
    }

    public Vector2 CalcEdgeToScenePos(int index)
    {
        var angle_deg = 60f * index;
        var angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector2(m_transform.position.x + m_size * Mathf.Cos(angle_rad),
                            m_transform.position.z + m_size * Mathf.Sin(angle_rad));
    }

    public Vector2 GetEdgePos(int index)
    {
        var angle_deg = 60f * index;
        var angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector2(m_coordX + m_size * 0.5f + m_size * Mathf.Cos(angle_rad),
                            m_coordY + m_size * 0.5f - m_size * Mathf.Sin(angle_rad));
    }

    public void SetNextType()
    {
        if (++Type >= HexType.Count)
        {
            Type = 0;
        }
    }

    public void SetPrevType()
    {
        Type = PrevType;
    }

    public bool IsValidForLoS()
    {
        return (type == HexType.Empty || type == HexType.Base);
    }

    private void OnDrawGizmos()
    {
        if (!HexagonManager.m_drawDebug)
            return;

        // Draw hexagon.

        Gizmos.color = Color.black;
        Vector3 orig = new Vector3(CalcEdgeToScenePos(5).x, 0f, CalcEdgeToScenePos(5).y);
        Vector3 dest = new Vector3(CalcEdgeToScenePos(0).x, 0f, CalcEdgeToScenePos(0).y);
        Gizmos.DrawLine(orig, dest);
        for (int i = 0; i < 5; i++)
        {
            orig = new Vector3(CalcEdgeToScenePos(i).x, 0f, CalcEdgeToScenePos(i).y);
            dest = new Vector3(CalcEdgeToScenePos(i + 1).x, 0f, CalcEdgeToScenePos(i + 1).y);

            Gizmos.DrawLine(orig, dest);
        }

        // Draw type of hexagon.

        if (Type == HexType.Base)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(m_transform.position, m_size * 0.9f);
            //Gizmos.DrawCube(m_transform.position, Vector3.one * m_size);
        }

        if (Type == HexType.Character)
        {
            Gizmos.color = Color.cyan;
            //Gizmos.DrawCube(m_transform.position, Vector3.one * m_size);
            Gizmos.DrawSphere(m_transform.position, m_size * 0.5f);
        }

        if (Type == HexType.Wall)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(m_transform.position, m_size * 0.9f);
            //Gizmos.DrawCube(m_transform.position, Vector3.one * m_size);
        }

        if (Type == HexType.Obstacle)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(m_transform.position, Vector3.one * m_size);
        }
    }

    private void Start()
    {
        m_transform = GetComponent<Transform>();

        BoxCollider col = gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(m_size * 1.1f, 0f, m_size * 1.1f);
    }
}
