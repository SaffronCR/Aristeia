// Great info about hexagons here: https://www.redblobgames.com/grids/hexagons/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public enum HexType
{
    Empty,
    Base,
    Character,
    Wall,
    Obstacle,

    Count
}

public enum Mode
{
    None,
    Edit,
    LoS,
    Character,
}

public enum Scenario
{
    None,
    Assault,
    Conquest,
    KingOfTheHill,
    ScorchedEarth,
    Blitz,
    Carnage,
}

public class HexagonManager : MonoBehaviour
{
    public GameObject m_hexPrefab = null;
    public float m_hexSize = 1f;
    public Transform m_mapParent = null;
    public int m_mapSize = 9;

    public static bool m_drawDebug = false;

    private const int m_kEdgeCount = 6;
    private const float m_kMinDistLoS = 0.1f;

    private Mode m_currentMode = Mode.None;
    private Scenario m_currentScenario = Scenario.None;
    private Hexagon m_dest;
    private bool m_drawAllLinesLoS = true;
    private List<Hexagon> m_hexagons = null;
    private bool[,] m_isEdgeLoSBlocked = new bool[m_kEdgeCount, m_kEdgeCount];
    private HexType[,] m_mapData = null;
    private Hexagon m_orig;
    private int m_bestEdgeIndex = 0;
    private int m_totalBlockCount = 0;

    public bool IsBetween(Vector2 a, Vector2 b, Vector2 c)
    {
        double dx = b.x - a.x;
        double dy = b.y - a.y;
        double innerProduct = (c.x - a.x) * dx + (c.y - a.y) * dy;

        return 0 <= innerProduct && innerProduct <= dx * dx + dy * dy;
    }

    public void SetAsssault()
    {
        m_currentScenario = Scenario.Assault;

        LoadMap();
        UpdateHexagon();
    }

    public void SetBlitz()
    {
        m_currentScenario = Scenario.Blitz;

        LoadMap();
        UpdateHexagon();
    }

    public void SetCarnage()
    {
        m_currentScenario = Scenario.Carnage;

        LoadMap();
        UpdateHexagon();
    }

    public void SetCharacterMode()
    {
        m_currentMode = Mode.Character;
    }

    public void SetConquest()
    {
        m_currentScenario = Scenario.Conquest;

        LoadMap();
        UpdateHexagon();
    }

    public void SetEditMode()
    {
        m_currentMode = Mode.Edit;
    }

    public void SetKingOfTheHill()
    {
        m_currentScenario = Scenario.KingOfTheHill;

        LoadMap();
        UpdateHexagon();
    }

    public void SetLoSMode()
    {
        if (m_currentMode == Mode.LoS)
        {
            m_drawAllLinesLoS = !m_drawAllLinesLoS;
        }

        m_currentMode = Mode.LoS;
    }

    public void SetScorchedEarth()
    {
        m_currentScenario = Scenario.ScorchedEarth;

        LoadMap();
        UpdateHexagon();
    }

    private bool CheckLoSBetweenEdges(int origEdge, int destEdge)
    {
        Vector2 origEdgeVec = m_orig.CalcEdgeToScenePos(origEdge);
        Vector2 destEdgeVec = m_dest.CalcEdgeToScenePos(destEdge);

        for (int i = 0; i < m_hexagons.Count; i++)
        {
            // Check only if the hex is different from orig and dest and can block LoS.
            if (m_hexagons[i] != m_orig && m_hexagons[i] != m_dest && m_hexagons[i].BlocksLoS() &&
                IsBetween(origEdgeVec, destEdgeVec, m_hexagons[i].CalcCenterToScenePos()))
            {
                // Check if the edge point is between the segment of the line.
                bool isBetween = false;
                for (int j = 0; j < m_kEdgeCount && !isBetween; j++)
                {
                    isBetween = IsBetween(origEdgeVec, destEdgeVec, m_hexagons[i].CalcEdgeToScenePos(j));
                }

                // Check if this hex has points in both sides of the line.
                if (isBetween)
                {
                    int leftCount = 0;
                    int rightCount = 0;

                    for (int j = 0; j < m_kEdgeCount; j++)
                    {
                        Vector2 currentHexEdgeVec = m_hexagons[i].CalcEdgeToScenePos(j);

                        // Don't compare if the position is the same!
                        if (currentHexEdgeVec != destEdgeVec && currentHexEdgeVec != origEdgeVec)
                        {
                            double d = (currentHexEdgeVec.x - origEdgeVec.x) * (destEdgeVec.y - origEdgeVec.y)
                                    - (currentHexEdgeVec.y - origEdgeVec.y) * (destEdgeVec.x - origEdgeVec.x);

                            if (d < -m_kMinDistLoS)
                            {
                                leftCount++;
                            }
                            else if (d > m_kMinDistLoS)
                            {
                                rightCount++;
                            }
                        }
                    }

                    // If this hex has points each side of the line, then it's blocking the LoS.
                    if (leftCount > 0 && rightCount > 0)
                    {
                        return true;
                    }

                    // #CRIS TODO: if it's all left or all right, we need to check if there's an adjacent block in the inverse case, as a hotfix for the "special case"
                    if (leftCount > 0 && rightCount == 0)
                    {
                        //Debug.LogWarning("TODO CHECK");
                    }
                    else if (rightCount > 0 && leftCount == 0)
                    {
                        //Debug.LogWarning("TODO CHECK");
                    }
                }
            }
        }

        return false;
    }

    private void CheckLoSBetweenHexagons()
    {
        if (m_currentMode != Mode.LoS)
        {
            return;
        }

        if (m_orig != null && m_orig.IsValidForLoS() &&
            m_dest != null && m_dest.IsValidForLoS())
        {
            m_bestEdgeIndex = 9999;
            m_totalBlockCount = m_kEdgeCount;

            for (int i = 0; i < m_kEdgeCount; i++)
            {
                int blockCount = 0;

                for (int j = 0; j < m_kEdgeCount; j++)
                {
                    m_isEdgeLoSBlocked[i, j] = CheckLoSBetweenEdges(i, j);

                    if (m_isEdgeLoSBlocked[i, j])
                    {
                        blockCount++;
                    }
                }

                if (blockCount < m_totalBlockCount)
                {
                    m_totalBlockCount = blockCount;
                    m_bestEdgeIndex = i;
                }
            }

            // failsafe.
            if (m_bestEdgeIndex > m_kEdgeCount)
            {
                Debug.LogError("m_origIndex not set!");
                m_bestEdgeIndex = 0;
            }
        }
    }

    private void LoadMap()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.dataPath + "/Resources/" + m_currentScenario.ToString() + ".sav", FileMode.Open);
        m_mapData = (HexType[,])bf.Deserialize(file);
        file.Close();
        Debug.Log("Loaded Data");
    }

    private void OnDrawGizmos()
    {
        //if (!m_drawDebug)
        //    return;

        if (m_orig != null && m_dest != null)
        {
            if (m_drawAllLinesLoS)
            {
                for (int i = 0; i < m_kEdgeCount; i++)
                {
                    Gizmos.color = m_isEdgeLoSBlocked[m_bestEdgeIndex, i] ? Color.red : Color.white;

                    Vector3 orig = new Vector3(m_orig.CalcEdgeToScenePos(m_bestEdgeIndex).x, 0f, m_orig.CalcEdgeToScenePos(m_bestEdgeIndex).y);
                    Vector3 dest = new Vector3(m_dest.CalcEdgeToScenePos(i).x, 0f, m_dest.CalcEdgeToScenePos(i).y);

                    Gizmos.DrawLine(orig, dest);
                }
            }
            else
            {
                if (m_totalBlockCount >= m_kEdgeCount - 1)
                {
                    Gizmos.color = Color.red;
                }
                else if (m_totalBlockCount > 0)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                Gizmos.DrawLine(m_orig.transform.position, m_dest.transform.position);
            }
        }
    }

    private void ResetLoS()
    {
        m_orig = null;
        m_dest = null;
    }

    private void SaveMap()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.dataPath + "/Resources/" + m_currentScenario.ToString() + ".sav");
        bf.Serialize(file, m_mapData);
        file.Close();
        Debug.Log("Saved Data");
    }

    private void Start()
    {
        m_mapData = new HexType[m_mapSize * 2 + 1, m_mapSize * 2 + 1];

        m_currentMode = Mode.LoS;
        m_currentScenario = Scenario.Assault;

        m_hexagons = new List<Hexagon>();

        LoadMap();
        UpdateHexagon();
    }

    private void Update()
    {
        if (m_currentMode == Mode.LoS)
        {
            UpdateLoS();
        }
        else if (m_currentMode == Mode.Edit)
        {
            UpdateEdit();
        }
        else if (m_currentMode == Mode.Character)
        {
            UpdateCharacter();
        }
    }

    private void UpdateCharacter()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Hexagon hexagon = hit.transform.GetComponent<Hexagon>();
                if (hexagon != null)
                {
                    if (hexagon.Type == HexType.Empty || hexagon.Type == HexType.Base)
                    {
                        hexagon.Type = HexType.Character;
                    }
                    else if (hexagon.Type == HexType.Character)
                    {
                        hexagon.SetPrevType();
                    }
                }
            }
        }
    }

    private void UpdateEdit()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveMap();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadMap();
            UpdateHexagon();
        }
#endif

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Hexagon hexagon = hit.transform.GetComponent<Hexagon>();
                if (hexagon != null)
                {
                    hexagon.SetNextType();

                    m_mapData[m_mapSize + hexagon.m_coordX, m_mapSize + hexagon.m_coordY] = hexagon.Type;
                }
            }
        }
    }

    private void UpdateHexagon()
    {
        for (int i = 0; i < m_hexagons.Count; i++)
        {
            if (m_hexagons[i] != null)
            {
                Destroy(m_hexagons[i].gameObject);
            }
        }
        m_hexagons.Clear();

        if (m_hexPrefab != null && m_mapParent != null)
        {
            float height = Mathf.Sqrt(3f) * m_hexSize;

            for (int i = -m_mapSize; i <= m_mapSize; ++i)
            {
                for (int j = -m_mapSize; j <= m_mapSize; ++j)
                {
                    if (Mathf.Abs(i + j) <= m_mapSize)
                    {
                        GameObject hexagon = Instantiate(m_hexPrefab, m_mapParent);
                        if (hexagon != null)
                        {
                            float x = 2f * m_hexSize * 0.75f * i;

                            float y = (height * j) + (height * 0.5f * i);

                            hexagon.transform.position = new Vector3(x, 0f, -y);

                            Hexagon hexComp = hexagon.GetComponent<Hexagon>();
                            if (hexComp != null)
                            {
                                hexComp.m_coordX = i;
                                hexComp.m_coordY = j;
                                hexComp.m_size = m_hexSize;
                                hexComp.Type = m_mapData[m_mapSize + i, m_mapSize + j];
                                hexComp.PrevType = m_mapData[m_mapSize + i, m_mapSize + j];

                                m_hexagons.Add(hexComp);
                            }
                        }
                    }
                }
            }
        }
    }

    private void UpdateLoS()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Hexagon hexagon = hit.transform.GetComponent<Hexagon>();
                if (hexagon != null && hexagon.IsValidForLoS())
                {
                    if (m_orig == null)
                    {
                        m_orig = hexagon;
                    }
                    else if (m_dest == null && m_orig != hexagon)
                    {
                        m_dest = hexagon;

                        CheckLoSBetweenHexagons();
                    }
                    else
                    {
                        ResetLoS();
                    }
                }
                else
                {
                    ResetLoS();
                }
            }
            else
            {
                ResetLoS();
            }
        }
    }
}
