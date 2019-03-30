using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Diagnostics;

public class GameManager : MonoBehaviour
{
    List<ObjectController> selectableObjects = new List<ObjectController>();
    List<ObjectController> selectedObjects = new List<ObjectController>();
    readonly int maxSelectedObjects = 16;
    List<RawImage> onMiniMap = new List<RawImage>();
    readonly int numberOfSpecificGroup = 10;
    List<ObjectController>[] specificGroup;

    Vector3 preMousePosition = new Vector3();
    Vector3 mousePosition = new Vector3();
    ObjectController preTarget = null;

    public Texture2D defaultCursor;
    public Texture2D CursorOnMine;
    public Texture2D CursorOnAlly;
    public Texture2D CursorOnEnemy;
    public Texture2D targetCursorOnMine;
    public Texture2D targetCursorOnAlly;
    public Texture2D targetCursorOnEnemy;
    enum CursorMode { Default, OnUnit, OnTarget, Drag };
    CursorMode cursorMode;
    enum CursorPosition { Default, OnMine, OnAlly, OnEnemy };
    CursorPosition cursorPosition;

    public RawImage thumbnail;
    public Text name_UI;
    public Text HP_UI;
    public Text Status_Menu;
    public Text Status_UI;

    public GameObject producing;
    public Slider produceProgressBar;
    public RawImage[] produceList;
    public Text producingGuide;

    public RawImage[] unitList;
    public RawImage[] commandImageList;
    public Texture stopTexture;
    public Texture attackTexture;
    public Texture holdTexture;

    public int maxResource;
    int resourceForPrint;
    public int startResource;
    float realResource;
    public int chargeResourcePerSecond;
    public Slider resourceBar;
    public Text resourceGuide;

    Camera mCamera;

    int playerNumber;

    public GameObject map;
    public RawImage minimap;
    public RawImage unitOnMinimap;
    Minimap minimapController;

    bool onAttackCommand = false;
    bool onBuildCommand = false;
    Structure structureForBuild = null;
    GameObject buildCursor;
    string ownerForBuild = null;
    Vector3 positionDeviationForBuild = Vector3.zero;
    public GameObject underBuild;

    public GameObject rallyPoint;
    GameObject rallyPointInstance;

    public Text frame;

    public Text messageBox;
    IEnumerator printMessageCoroutine;

    void Awake()
    {
        mCamera = Camera.main;

        // 플레이어 설정
        playerNumber = 4;
        Global.players = new List<string>();
        Global.team = new List<List<string>>
        {
            new List<string>()
        };
        Global.players.Add("Enemy");
        for (int i = 0; i < playerNumber; i++)
        {
            Global.players.Add("Player" + (i + 1));
            Global.team[0].Add("Player" + (i + 1));
        }
        Global.team.Add(new List<string>());
        Global.team[1].Add("Enemy");
        Global.playerName = Global.players[1];

        // objectControllers(씬에 존재하는 모든 선택가능 오브젝트 리스트) 초기화
        GameObject[] objectList = GameObject.FindGameObjectsWithTag("SelectableObject");
        for (int i = 0; i < objectList.Length; i++)
        {
            selectableObjects.Add(objectList[i].GetComponent<ObjectController>());
            selectableObjects[i].Death += DeleteDeathObject;
            if (selectableObjects[i].GetType() == typeof(Structure))
            {
                ((Structure)selectableObjects[i]).Produce += CreateObject;
                ((Structure)selectableObjects[i]).Build += BuildStructure;
            }
            AddOnMiniMap(selectableObjects[i]);
        }
    }

    void Start()
    {
        cursorMode = CursorMode.Default;
        cursorPosition = CursorPosition.Default;

        minimapController = minimap.GetComponent<Minimap>();
        minimapController.Move += MoveOrder;
        minimapController.ShowRallyPoint += ShowRallyPoint;
        minimapController.map = map;

        realResource = startResource;

        SetMousePosition();

        onAttackCommand = true;
        OnAttack(false);

        // 마우스 가두기
        Cursor.lockState = CursorLockMode.Confined;

        specificGroup = new List<ObjectController>[numberOfSpecificGroup];
        for(int i = 0; i < numberOfSpecificGroup; i++)
        {
            specificGroup[i] = new List<ObjectController>();
        }

        StartCoroutine(PrintFrame());

        CameraToLookPoint(map.transform.position);
    }

    bool isDragStartInUI = false;
    void Update()
    {
        if (onAttackCommand) cursorMode = CursorMode.OnTarget;
        else cursorMode = CursorMode.Default;
        Cursor.visible = true;

        // ESC 키 - 현재 동작 취소, 생산 취소 (건물 취소는 건물 건설 코루틴에서 함)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (onBuildCommand)
                onBuildCommand = false;
            else if (onAttackCommand)
                onAttackCommand = false;
            else
            {
                foreach (ObjectController obj in selectedObjects)
                {
                    if (obj.IsStructure()) ((Structure)obj).CancelLastProducing();
                }
            }
        }

        // 숫자 키 - 부대 선택
        // 부대를 선택하면 현재 동작은 모두 취소됨
        for (int i = 0; i < 10; i++)
        {
            // 부대 선택
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && specificGroup[i].Count > 0)
            {
                if (onBuildCommand)
                    onBuildCommand = false;
                else if (onAttackCommand)
                    onAttackCommand = false;

                DeselectAll();
                for(int j = 0; j < specificGroup[i].Count; j++)
                {
                    SelectObject(specificGroup[i][j]);
                }
            }
        }

        if (!onBuildCommand)
        {
            /* 일반적인 마우스 클릭 처리 */

            Destroy(buildCursor);

            // 좌클릭
            // 왼쪽 버튼을 눌렀을 땐 위치만 저장하고 진짜 동작은 왼쪽 버튼을 뗐을 때 시작한다.
            if (Input.GetMouseButtonDown(0))
            {
                preMousePosition = Input.mousePosition;
                preTarget = GetClickedObject();
                if (EventSystem.current.IsPointerOverGameObject()) isDragStartInUI = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (onAttackCommand && !isDragStartInUI)
                {
                    if (preTarget != null && GetClickedObject() == preTarget && IsSelectable(preTarget))
                    {
                        // 강제 공격
                        ObjectController target = preTarget;

                        foreach (ObjectController obj in selectedObjects)
                            if (target != null) obj.ForcedAttack(target);
                    }
                    else
                    {
                        AttackMoveOrder(mousePosition);
                    }
                    OnAttack(false);
                }
                else
                {
                    if (!(isDragStartInUI && EventSystem.current.IsPointerOverGameObject()))
                    {
                        // 왼쪽 시프트 다중 선택
                        if (!Input.GetKey(KeyCode.LeftShift))
                            DeselectAll();

                        if (preTarget != null && GetClickedObject() == preTarget && IsSelectable(preTarget))
                        {
                            // 단일 유닛 선택하기
                            if (Input.GetKey(KeyCode.LeftControl)) SelectOnScreen(preTarget);
                            else
                            {
                                if (Input.GetKey(KeyCode.LeftShift) && preTarget.IsSelected()) DeselectObject(preTarget);
                                else SelectObject(preTarget);
                            }
                        }
                        else
                        {
                            // 다중 유닛 선택하기
                            SelectObjects();
                        }

                        // 선택 결과 체크
                        // 첫 번째 오브젝트 이외에는 모두 자신 소유여야 한다.
                        for (int i = 1; i < selectedObjects.Count; i++)
                        {
                            if (!IsMine(selectedObjects[i]))
                            {
                                // 선택된 유닛 중 자신 소유가 아닌 것 포함 -> 해당 유닛을 제거한다.
                                DeselectObject(selectedObjects[i]);
                            }
                        }
                        // 첫 번째 오브젝트 체크
                        // 첫 번째 오브젝트는 자신 소유거나 혼자 선택된 경우에만 선택을 유지한다.
                        if (selectedObjects.Count > 1 && !IsMine(selectedObjects[0]))
                        {
                            DeselectObject(selectedObjects[0]);
                        }
                    }

                    isDragStartInUI = false;
                }
            }

            // 좌드래그
            if (Input.GetMouseButton(0))
            {
                if (!onAttackCommand && !EventSystem.current.IsPointerOverGameObject())
                {
                    cursorMode = CursorMode.Drag;
                }
            }

            // 우클릭
            if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (onAttackCommand) onAttackCommand = false;
                else if (selectedObjects.Count > 0 && IsMine(selectedObjects[0]))
                {
                    // 적군 유닛 한정 강제 공격. 우클릭한 대상이 적군이 아니라면 마우스 위치로 이동한다.
                    ObjectController target = GetClickedObject();
                    if (IsSelectable(target) && Global.Relation(Global.playerName, target.owner) == Team.Enemy)
                    {
                        foreach (ObjectController obj in selectedObjects)
                        {
                            obj.ForcedAttack(target);
                        }
                    }
                    else
                        MoveOrder(mousePosition);

                    OnAttack(false);

                    foreach (ObjectController obj in selectedObjects)
                    {
                        if (!obj.IsUnit())
                        {
                            Structure structure = (Structure)obj;
                            if (target == obj)
                                structure.SetRallyPoint(obj.transform.position);
                            else
                                structure.SetRallyPoint(mousePosition);
                            ShowRallyPoint();
                        }
                    }
                }
            }

            // 단축키 A
            if (Input.GetKeyDown(KeyCode.A) && selectedObjects.Count > 0 && IsMine(selectedObjects[0]))
            {
                AttackOrder();
            }

            // 단축키 S
            if (Input.GetKeyDown(KeyCode.S) && selectedObjects.Count > 0 && IsMine(selectedObjects[0]))
            {
                StopOrder();
            }

            // 단축키 H
            if (Input.GetKeyDown(KeyCode.H) && selectedObjects.Count > 0 && IsMine(selectedObjects[0]))
            {
                HoldOrder();
            }

            // 부대 지정 (부대가 10개를 넘으면 별도 처리 필요)
            for (int i = 0; i < 10; i++)
            {
                // 새 부대 지정
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    specificGroup[i].Clear();
                    for (int j = 0; j < selectedObjects.Count && specificGroup[i].Count <= maxSelectedObjects; j++)
                    {
                        if (IsMine(selectedObjects[j]))
                            specificGroup[i].Add(selectedObjects[j]);
                    }
                }

                // 기존 부대에 추가
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    for (int j = 0; j < selectedObjects.Count && specificGroup[i].Count <= maxSelectedObjects; j++)
                    {
                        if (IsMine(selectedObjects[j]))
                            specificGroup[i].Add(selectedObjects[j]);
                    }
                }
            }
        }
        else
        {
            /* 건물 건설 모드의 마우스 클릭 처리 */

            buildCursor.transform.position = (mCamera.ScreenToWorldPoint(Input.mousePosition) + mCamera.transform.forward * 100);

            if (!EventSystem.current.IsPointerOverGameObject())
            {
                Cursor.visible = false;

                SetMousePosition(1 << LayerMask.NameToLayer("Plane"));

                BoxCollider structBox = structureForBuild.GetComponent<BoxCollider>();
                Vector3 buildPosition = mousePosition - mCamera.transform.forward * structBox.size.y / 2 / Mathf.Sin(mCamera.transform.rotation.eulerAngles.x * Mathf.Deg2Rad);
                Collider[] inBuildPosition = Physics.OverlapBox(buildPosition + Vector3.up * structBox.center.y, structBox.size / 2, buildCursor.transform.rotation);

                bool buildable = true;
                SetBuildCursorAlpha(200);
                int blinkCycle = 10;
                for (int i = 0; i < inBuildPosition.Length; i++)
                {
                    if (inBuildPosition[i].GetComponent<ObjectController>() != null)
                    {
                        buildable = false;
                        SetBuildCursorAlpha(200 / (blinkCycle - 1) * (1 + Time.frameCount % blinkCycle));
                        break;
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (buildable)
                    {
                        realResource -= structureForBuild.resource;
                        CreateObject(1, buildPosition, structureForBuild, Global.playerName);
                        onBuildCommand = false;
                    }
                    else
                    {
                        PrintMessage("건설할 수 없습니다.");
                    }
                }

                if (Input.GetMouseButtonDown(1))
                {
                    onBuildCommand = false;
                }
            }
        }


        ControlMainCamera();
        SetMouseCursor();
        SetMiniMap();
        SetMousePosition();
        SetUIWithSelectedUnit();
        SetCommandWithSelectedUnit();

        realResource += chargeResourcePerSecond * Time.deltaTime;
        if (realResource > maxResource) realResource = maxResource;
        resourceBar.value = realResource / maxResource;
        resourceForPrint = (int)realResource;
        resourceGuide.text = resourceForPrint.ToString();

        if (selectedObjects.Count == 0)
        {
            OnAttack(false);
        }
    }

    IEnumerator PrintFrame()
    {
        float startTime;
        int startFrame;

        frame.text = "Fps : " + (1.0f / Time.deltaTime).ToString("N2");

        while (true)
        {
            startTime = Time.time;
            startFrame = Time.frameCount;

            while(Time.time - startTime < 1)
            {
                yield return null;
            }

            frame.text = "Fps : " + ((Time.frameCount - startFrame) / (Time.time - startTime)).ToString("N2");
            yield return null;
        }
    }

    /**********************************************************
     * 공격 모드 변경
     * 공격 모드일 경우 미니맵에 공격 델리게이트를 할당한다. (미니맵 클릭으로 이동 공격)
     * 공격 모드 해제일 경우 미니맵에 카메라 이동 델리게이트를 할당한다. (미니맵 클릭으로 카메라 이동)
     * 반대쪽 델리게이트는 해제한다.
     * 우클릭은 항상 등록되어 있음.
     * 파라미터 isOnAttack : 공격 모드 발동 / 해제
     *********************************************************/
    public void OnAttack(bool isOnAttack)
    {
        if (onAttackCommand != isOnAttack)
        {
            if (isOnAttack)
            {
                minimapController.AttackMove += AttackMoveOrder;
                minimapController.CameraToLookPoint -= CameraToLookPoint;
            }
            else
            {
                minimapController.AttackMove -= AttackMoveOrder;
                minimapController.CameraToLookPoint += CameraToLookPoint;
            }
        }
        onAttackCommand = isOnAttack;
    }

    /**********************************************************
     * 명령 : 이동
     * 선택된 모든 유닛에게 이동 명령 전달
     *********************************************************/
    public void MoveOrder(Vector3 point)
    {
        foreach (ObjectController obj in selectedObjects)
            if(obj.onReceiveCommand) obj.Move(point);
    }

    /**********************************************************
     * 명령 : 이동 공격
     * 선택된 모든 유닛에게 이동 공격 명령 전달
     *********************************************************/
    public void AttackMoveOrder(Vector3 point)
    {
        foreach (ObjectController obj in selectedObjects)
            if (obj.onReceiveCommand && obj.Attackable()) obj.AttackMove(point);
        OnAttack(false);
    }

    /**********************************************************
     * 명령 : 공격
     * 선택된 유닛 중 공격 가능한 유닛이 있으면 마우스를 공격 모드로 전환
     *********************************************************/
    public void AttackOrder()
    {
        bool isAttackable = false;
        for (int i = 0; i < selectedObjects.Count; i++)
        {
            if (selectedObjects[i].onReceiveCommand && selectedObjects[i].Attackable()) isAttackable = true;
        }
        if (isAttackable) OnAttack(true);
    }

    /**********************************************************
     * 명령 : 정지
     * 선택된 모든 유닛에게 정지 명령 전달
     *********************************************************/
    public void StopOrder()
    {
        foreach (ObjectController obj in selectedObjects)
        {
            if (obj.onReceiveCommand) obj.Stop();
        }
    }

    /**********************************************************
     * 명령 : 위치 사수
     * 선택된 모든 유닛에게 위치 사수 명령 전달
     *********************************************************/
    public void HoldOrder()
    {
        foreach (ObjectController obj in selectedObjects)
        {
            if (obj.onReceiveCommand) obj.Hold();
        }
    }

    /**********************************************************
     * 유닛 및 건물 소환
     * 파라미터 number : 소환할 개수
     * 파라미터 position : 소환할 위치
     * 파라미터 unit : 소환할 유닛
     * 파라미터 owner : 소환할 유닛의 소유자
     * 파라미터 hasRallyPoint : 생산 직후 향할 랠리 포인트의 존재 여부
     * 파라미터 rallyPoint : 랠리 포인트
     *********************************************************/
    void CreateObject(int number, Vector3 position, ObjectController unit, string owner, bool hasRallyPoint = false, Vector3 rallyPoint = new Vector3())
    {
        for (int i = 0; i < number; i++)
        {
            GameObject newObject = Instantiate(unit.gameObject, position, Quaternion.identity);
            ObjectController objc = newObject.GetComponent<ObjectController>();

            objc.owner = owner;
            objc.team = Global.Relation(owner, Global.playerName);
            objc.Death += DeleteDeathObject;

            if (!objc.IsUnit())
            {
                ((Structure)objc).Produce += CreateObject;
                ((Structure)objc).CheckResource += CheckResource;
                ((Structure)objc).Build += BuildStructure;
                StartCoroutine(BuildStructureCoroutine(position, (Structure) objc, owner));
            }

            selectableObjects.Add(objc);
            AddOnMiniMap(objc);

            if (hasRallyPoint)
            {
                if (objc.Attackable()) objc.AttackMove(rallyPoint);
                else objc.Move(rallyPoint);
            }
        }

        if(unit.IsUnit())
            PrintMessage(unit.unitName + " 생산 완료");
    }

    /**********************************************************
     * Structure 클래스의 유닛 생산 / 취소로 할당되는 콜백 함수
     * 파라미터 unit : 생산 / 취소 대상 유닛
     * resource : 현재 총 자원에 더해질 값(생산은 양수, 취소는 음수)
     * queue : 생산 건물의 유닛 생산 리스트. 자원을 체크하고 반영한 뒤, 큐에 유닛을 더하거나 큐에서 삭제한다.
     *********************************************************/
    void CheckResource(Unit unit, int resource, List<Unit> queue)
    {
        if (realResource + resource < 0)
        {
            queue.Remove(unit);
            PrintMessage("자원이 부족합니다.");
        } else
        {
            realResource += resource;
        }
    }

    /**********************************************************
     * 죽은 유닛을 오브젝트 목록에서 제거한다
     * 이 메서드는 각 오브젝트에 Delegate로 포함된다.
     * 파라미터 deathObject : 죽은 유닛 (ObjectContoller에서 this를 파라미터로 넘겨준다)
     *********************************************************/
    void DeleteDeathObject(ObjectController deathObject)
    {
        if (selectableObjects.Contains(deathObject))
        {
            RemoveFromMiniMap(onMiniMap[selectableObjects.IndexOf(deathObject)]);
            selectableObjects.Remove(deathObject);
            selectedObjects.Remove(deathObject);
            Destroy(deathObject.gameObject);
        }
    }

    /**********************************************************
     * 건물 건설 모드 돌입
     * 마우스 커서를 건설할 건물로 바꾸고 건물 건설 bool 변수를 true로 바꾼다
     * 파라미터 structure : 건설할 건물
     * 파라미터 owner : 건설할 건물의 소유자
     *********************************************************/
    void BuildStructure(Structure structure, string owner)
    {
        Destroy(buildCursor);

        if (realResource < structure.resource)
        {
            PrintMessage("자원이 부족합니다");
        }
        else
        {
            onBuildCommand = true;
            structureForBuild = structure;
            ownerForBuild = owner;
            GameObject buildIcon = structure.transform.Find("Graphic").gameObject;
            buildCursor = Instantiate(buildIcon);
            positionDeviationForBuild = buildIcon.transform.position;

            buildCursor.transform.rotation = structure.baseRotationAngle;
        }
    }

    /**********************************************************
     * 건물 건설 모드에서 건물 커서의 투명도 조절
     * 파라미터 alpha : 투명도
     *********************************************************/
    void SetBuildCursorAlpha(float alpha)
    {
        Transform[] childList = buildCursor.GetComponentsInChildren<Transform>();
        foreach (Transform child in childList)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Effect");
            MeshRenderer meshRenderer = child.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                //meshRenderer.material.shader = Shader.Find("Transparent/Diffuse");
                meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, alpha / 255f);
            }
        }
    }

    /**********************************************************
     * 건물 건설 코루틴
     * 시간 만족 시 건물이 완성됨
     *********************************************************/
    IEnumerator BuildStructureCoroutine(Vector3 position, Structure forBuild, string owner)
    {
        float startBuildTime = Time.time;
        int maxHitPoint = forBuild.hitPoint;
        forBuild.onReceiveCommand = false;

        GameObject underBuild = forBuild.transform.Find("UnderBuildGraphic").gameObject;
        underBuild.SetActive(true);
        GameObject afterFinish = forBuild.transform.Find("Graphic").gameObject;
        afterFinish.SetActive(false);

        while (Time.time - startBuildTime < forBuild.produceTime)
        {
            if (forBuild.IsSelected() && Input.GetKeyUp(KeyCode.Escape))
            {
                realResource += forBuild.resource;
                forBuild.ExplodeObject();
                yield break;
            }

            forBuild.objectMakingPercentage = (Time.time - startBuildTime) / forBuild.produceTime;

            yield return null;
        }

        forBuild.objectMakingPercentage = 1;
        underBuild.SetActive(false);
        afterFinish.SetActive(true);
        forBuild.SetHitPoint(maxHitPoint);
        forBuild.SetEnableCommand(true);
        PrintMessage(forBuild.unitName + " 건설 완료");
    }

    /**********************************************************
     * 마우스 포인터 위치 및 커서 설정.
     * 마우스가 가리키는 오브젝트 없으면 변화 없음.
     *********************************************************/
    void SetMousePosition(int layerMask = Physics.DefaultRaycastLayers)
    {
        Ray ray = mCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            mousePosition = hit.point;
            ObjectController unitBehindCursor = hit.collider.gameObject.GetComponent<ObjectController>();

            cursorPosition = CursorPosition.Default;
            if (!EventSystem.current.IsPointerOverGameObject() && unitBehindCursor != null)
            {
                if (IsMine(unitBehindCursor))
                {
                    cursorPosition = CursorPosition.OnMine;
                }
                else if (Global.Relation(unitBehindCursor.owner, Global.playerName) == Team.Enemy)
                {
                    cursorPosition = CursorPosition.OnEnemy;
                }
                else
                {
                    cursorPosition = CursorPosition.OnAlly;
                }
            }
            if (onAttackCommand) cursorMode = CursorMode.OnTarget;
        }
    }

    /**********************************************************
     * 마우스로 클릭한 오브젝트 반환
     *********************************************************/
    ObjectController GetClickedObject()
    {
        Ray ray = mCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit))
        {
            ObjectController clickedObject = hit.collider.gameObject.GetComponent<ObjectController>();
            if (clickedObject != null) return clickedObject;
        }
        return null;
    }

    /**********************************************************
     * 파라미터 target이 선택 가능 오브젝트인지 여부
     *********************************************************/
    bool IsSelectable(ObjectController target)
    {
        if (target != null && target.tag == "SelectableObject") return true;
        else return false;
    }

    /**********************************************************
     * 선택 가능한 오브젝트 선택
     *********************************************************/
    void SelectObject(ObjectController target)
    {
        bool isSelectable = selectedObjects.Count < maxSelectedObjects && IsSelectable(target) && !selectedObjects.Contains(target);
        bool isNewSelect = selectedObjects.Count == 0;
        bool isUnitAll = selectedObjects.Count > 0 && selectedObjects[0].IsUnit() && target.IsUnit();
        bool isStructureAll = selectedObjects.Count > 0 && selectedObjects[0].IsStructure() && target.IsStructure() && selectedObjects[0].unitName == target.unitName;

        if (isSelectable && (isNewSelect || isUnitAll || isStructureAll))
        {
            target.SetSelected(true);
            selectedObjects.Add(target);
            selectedObjects.Sort((ObjectController a, ObjectController b) => b.resource.CompareTo(a.resource));

            if (target.IsStructure()) minimapController.SetRallyPoint += ((Structure)target).SetRallyPoint;

            ShowRallyPoint();
        }
    }

    /**********************************************************
     * 선택 가능한 오브젝트 선택 해제
     *********************************************************/
    void DeselectObject(ObjectController target)
    {
        if (IsSelectable(target))
        {
            target.SetSelected(false);
            selectedObjects.Remove(target);

            if (target.IsStructure()) minimapController.SetRallyPoint -= ((Structure)target).SetRallyPoint;
        }
    }

    /**********************************************************
     * 모든 유닛 선택 해제
     *********************************************************/
    void DeselectAll()
    {
        ObjectController[] tempList = selectedObjects.ToArray();
        for (int i = 0; i < tempList.Length; i++)
        {
            DeselectObject(tempList[i]);
        }
    }

    /**********************************************************
     * 다중 선택
     * BoxCastAll로 드래그 내 영역을 모두 투사한다
     *********************************************************/
    void SelectObjects()
    {
        Vector3[] point = new Vector3[4];
        Vector3[] realPoint = new Vector3[4];

        point[0] = mCamera.ScreenToWorldPoint(preMousePosition) + mCamera.transform.forward * 10;
        point[2] = mCamera.ScreenToWorldPoint(Input.mousePosition) + mCamera.transform.forward * 10;
        point[1] = mCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, preMousePosition.y, 0)) + mCamera.transform.forward * 10;
        point[3] = mCamera.ScreenToWorldPoint(new Vector3(preMousePosition.x, Input.mousePosition.y, 0)) + mCamera.transform.forward * 10;

        List<ObjectController> forSelect = new List<ObjectController>();
        bool selectMine = false;
        bool selectOnlyStructure = true;
        RaycastHit[] hitColliders = Physics.BoxCastAll((point[0] + point[2]) / 2, new Vector3(Vector3.Distance(point[0], point[1]), Vector3.Distance(point[0], point[3]), 10) / 2, mCamera.transform.forward, mCamera.transform.rotation);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            ObjectController obj = hitColliders[i].collider.gameObject.GetComponent<ObjectController>();
            if (IsSelectable(obj))
            {
                if (obj.owner == Global.playerName)
                {
                    selectMine = true;
                    if (obj.IsUnit())
                        selectOnlyStructure = false;
                }
                forSelect.Add(obj);
            }
        }

        // 선택 조건
        // 1. 선택 범위 내 자신이 소유한 오브젝트가 단 하나도 없음 : 아무거나 하나 선택하고 끝
        // 2. 선택 범위 내 자신이 소유한 건물은 있지만 유닛은 없음 : 건물 하나 선택하고 끝
        // 3. 선택 범위 내 자신이 소유한 유닛이 있음 : 그 유닛들만 선택
        if (!selectMine)
        {
            if (forSelect.Count > 0) SelectObject(forSelect[0]);
        }
        else
        {
            foreach (ObjectController obj in forSelect)
            {
                if (IsMine(obj))
                {
                    if (selectOnlyStructure || obj.IsUnit())
                        SelectObject(obj);
                    if (selectOnlyStructure) break;
                }
            }
        }
    }

    /**********************************************************
     * 화면 안의 모든 동종 오브젝트 선택
     *********************************************************/
    void SelectOnScreen(ObjectController target)
    {
        RaycastHit[] hitColliders = Physics.BoxCastAll(mCamera.transform.position, new Vector3(mCamera.orthographicSize * Screen.width / Screen.height, mCamera.orthographicSize, 10), mCamera.transform.forward, mCamera.transform.rotation);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            ObjectController obj = hitColliders[i].collider.GetComponent<ObjectController>();
            if (IsSelectable(obj) && obj.unitName.Equals(target.unitName))
            {
                SelectObject(obj);
            }
        }
    }

    /**********************************************************
     * 랠리 포인트 보여주기
     *********************************************************/
    void ShowRallyPoint()
    {
        if (selectedObjects.Count == 1 && selectedObjects[0].IsStructure() && ((Structure)selectedObjects[0]).hasRallyPoint)
        {
            Vector3 point = ((Structure)selectedObjects[0]).GetRallyPoint();
            if (rallyPointInstance == null)
                rallyPointInstance = Instantiate(rallyPoint, point, Quaternion.identity);
            else
                rallyPointInstance.transform.position = point;
        }
        else
        {
            if (rallyPointInstance != null) Destroy(rallyPointInstance);
        }
    }

    /**********************************************************
     * 카메라에 그림 그리기
     *********************************************************/
    private void OnGUI()
    {
        Rect rect;

        // 마우스 드래그 사각형
        if (cursorMode == CursorMode.Drag)
        {
            rect = SetRect(preMousePosition, Input.mousePosition);

            DrawScreenRect(rect, new Color(1f, 1f, 1f, 0.25f));
            DrawScreenRectBorder(rect, 2, new Color(1f, 1f, 1f));
        }

        // 미니맵에 현재 위치 표시
        float cameraLeft = mCamera.transform.position.x - mCamera.orthographicSize * Screen.width / Screen.height;
        float cameraRight = mCamera.transform.position.x + mCamera.orthographicSize * Screen.width / Screen.height;

        float degree = mCamera.transform.rotation.eulerAngles.x * Mathf.Deg2Rad;
        float cameraTop = mCamera.transform.position.z + (mCamera.transform.position.y / Mathf.Tan(degree) + mCamera.orthographicSize / Mathf.Sin(degree));
        float cameraBottom = mCamera.transform.position.z + (mCamera.transform.position.y / Mathf.Tan(degree) - mCamera.orthographicSize / Mathf.Sin(degree));

        cameraBottom += (cameraTop - cameraBottom) * Global.uiWidthHeightRadio;     // UI의 가로 : 세로 비율이 4:1이므로 1/4만큼을 제외하고 표시

        rect = SetRect(minimapController.GetMinimapPointFromWorldPoint(new Vector3(cameraLeft, 0, cameraTop)), minimapController.GetMinimapPointFromWorldPoint(new Vector3(cameraRight, 0, cameraBottom)));

        DrawScreenRectBorder(rect, 2, new Color(1f, 1f, 1f));
    }

    /**********************************************************
     * 사각형 좌표 설정
     * 왼쪽 위 좌표와 오른쪽 아래 좌표를 이용해 사각형을 만든다
     *********************************************************/
    Rect SetRect(Vector3 point1, Vector3 point2)
    {
        point1.y = Screen.height - point1.y;
        point2.y = Screen.height - point2.y;
        Vector3 topLeft = Vector3.Min(point1, point2);
        Vector3 bottomRight = Vector3.Max(point1, point2);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    /**********************************************************
     * 반투명한 사각형 그리기
     *********************************************************/
    public static void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        Texture2D whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = Color.white;
    }

    /**********************************************************
     * 안이 빈 사각형 그리기
     *********************************************************/
    public static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        // Top
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        // Left
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        // Right
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        // Bottom
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
    }

    /**********************************************************
     * 벡터 a와 b 사이의 각도 구하기
     * 각도는 가장 작은 각도 (0°~ 180°)만 구한다.
     *********************************************************/
    float GetAngleBetweenVector(Vector3 a, Vector3 b)
    {
        if (a.magnitude * b.magnitude == 0) return 0;
        float cosAngle = Vector3.Dot(a, b) / (a.magnitude * b.magnitude);
        float angle = Mathf.Acos(cosAngle);
        return angle;
    }

    /**********************************************************
     * 마우스 커서 설정
     * CursorMode : Default(기본 상태) OnTarget(공격, 스킬 시전 등 타겟 지정 상태)
     * CursorPosition : Default(아무것도 잡힌 게 없음) OnMine(아군 유닛 타겟 중) OnAlly(아군도 적군도 아님) OnEnemy(저놈을 죽이자)
     *********************************************************/
    void SetMouseCursor()
    {
        switch (cursorMode)
        {
            case CursorMode.Default:
                switch (cursorPosition)
                {
                    case CursorPosition.Default:
                        Cursor.SetCursor(defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto);
                        break;
                    case CursorPosition.OnMine:
                        Cursor.SetCursor(CursorOnMine, new Vector2(CursorOnMine.width / 2, CursorOnMine.height / 2), UnityEngine.CursorMode.Auto);
                        break;
                    case CursorPosition.OnAlly:
                        Cursor.SetCursor(CursorOnAlly, new Vector2(CursorOnAlly.width / 2, CursorOnAlly.height / 2), UnityEngine.CursorMode.Auto);
                        break;
                    case CursorPosition.OnEnemy:
                        Cursor.SetCursor(CursorOnEnemy, new Vector2(CursorOnEnemy.width / 2, CursorOnEnemy.height / 2), UnityEngine.CursorMode.Auto);
                        break;
                }
                break;
            case CursorMode.OnTarget:
                switch (cursorPosition)
                {
                    case CursorPosition.Default:
                        Cursor.SetCursor(targetCursorOnEnemy, new Vector2(CursorOnMine.width / 2, CursorOnMine.height / 2), UnityEngine.CursorMode.Auto);
                        break;
                    case CursorPosition.OnMine:
                        Cursor.SetCursor(targetCursorOnMine, new Vector2(CursorOnMine.width / 2, CursorOnMine.height / 2), UnityEngine.CursorMode.Auto);
                        break;
                    case CursorPosition.OnAlly:
                        Cursor.SetCursor(targetCursorOnAlly, new Vector2(CursorOnMine.width / 2, CursorOnMine.height / 2), UnityEngine.CursorMode.Auto);
                        break;
                    case CursorPosition.OnEnemy:
                        Cursor.SetCursor(targetCursorOnEnemy, new Vector2(CursorOnMine.width / 2, CursorOnMine.height / 2), UnityEngine.CursorMode.Auto);
                        break;
                }
                break;
            case CursorMode.Drag:
                Cursor.SetCursor(defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto);
                break;
        }
    }

    /**********************************************************
     * 유닛이 내 것인지 확인
     * 파라미터 unit : 대상 유닛
     *********************************************************/
    public static bool IsMine(ObjectController unit)
    {
        if (unit != null && unit.owner == Global.playerName) return true;
        else return false;
    }

    /**********************************************************
     * UI의 상태 정보창을 선택한 유닛으로 변경함
     * 선택된 유닛이 없을 경우 : 아무것도 표시 안함
     * 선택된 유닛이 하나일 경우 : 해당 유닛의 자세한 정보를 표시
     * 선택된 유닛이 여럿일 경우 : 썸네일 사진 나열
     *********************************************************/
    public void SetUIWithSelectedUnit()
    {
        if (selectedObjects.Count == 1)
        {
            selectedObjects[0].GetStatusForUI(out string unitName, out int hitPoint, out int currentHitPoint, out int damage, out int armor);

            thumbnail.gameObject.SetActive(true);
            thumbnail.texture = selectedObjects[0].thumbnail;
            name_UI.GetComponent<Text>().text = unitName;
            HP_UI.GetComponent<Text>().text = currentHitPoint + " / " + hitPoint;
            if (currentHitPoint <= hitPoint / 3) HP_UI.GetComponent<Text>().color = Color.red;
            else HP_UI.GetComponent<Text>().color = Color.white;

            Status_Menu.GetComponent<Text>().text = "";
            Status_UI.GetComponent<Text>().text = "";
            producing.gameObject.SetActive(false);

            // 건설 중 모델 (모든 건물은 건설 중 모델을 UnderBuildGraphic이라는 이름의 자식 오브젝트로 갖고 있음)
            Transform underBuildGraphic = selectedObjects[0].transform.Find("UnderBuildGraphic");
            if (!selectedObjects[0].IsStructure() || !underBuildGraphic.gameObject.activeSelf)
            {
                float produceProgress = 0;
                List<Unit> list = null;
                Structure selectedStructure = null;
                if (selectedObjects[0].IsStructure())
                {
                    selectedStructure = (Structure)selectedObjects[0];
                    selectedStructure.GetProduceList(out produceProgress, out list);
                }

                if (list != null && list.Count > 0)
                {
                    producing.gameObject.SetActive(true);
                    produceProgressBar.value = produceProgress;
                    producingGuide.text = "Producing " + list[0].unitName + " (" + (int)(produceProgress * 100) + "%)";
                    for (int i = 0; i < produceList.Length; i++)
                    {
                        produceList[i].gameObject.SetActive(true);
                        RawImage producingIcon = produceList[i].transform.GetChild(0).GetComponent<RawImage>();
                        Button producingButton = produceList[i].transform.GetChild(0).GetComponent<Button>();
                        producingButton.onClick.RemoveAllListeners();
                        if (i < list.Count)
                        {
                            int index = i;
                            producingIcon.gameObject.SetActive(true);
                            producingIcon.texture = list[i].thumbnail;
                            producingButton.onClick.AddListener(() => selectedStructure.CancelProducing(index));
                        }
                        else
                        {
                            produceList[i].transform.GetChild(0).gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    if (damage != 0)
                    {
                        Status_Menu.GetComponent<Text>().text = "Damage\n";
                        Status_UI.GetComponent<Text>().text = damage + "\n";
                    }
                    else
                    {
                        Status_Menu.GetComponent<Text>().text = "";
                        Status_UI.GetComponent<Text>().text = "";
                    }

                    Status_Menu.GetComponent<Text>().text += "Armor";
                    Status_UI.GetComponent<Text>().text += armor;
                }
            } else
            {
                producing.gameObject.SetActive(true);
                produceProgressBar.value = selectedObjects[0].objectMakingPercentage;
                producingGuide.text = "Constructing... (" + (int)(produceProgressBar.value * 100) + "%)";
                produceList[0].gameObject.SetActive(true);
                produceList[0].transform.GetChild(0).GetComponent<RawImage>().texture = selectedObjects[0].thumbnail;
                for (int i = 1; i < produceList.Length; i++)
                {
                    produceList[i].gameObject.SetActive(false);
                }
            }
            
        }
        else
        {
            thumbnail.gameObject.SetActive(false);
            name_UI.GetComponent<Text>().text = "";
            HP_UI.GetComponent<Text>().text = "";
            Status_Menu.GetComponent<Text>().text = "";
            Status_UI.GetComponent<Text>().text = "";
            producing.gameObject.SetActive(false);
        }

        for (int i = 0; i < unitList.Length; i++)
        {
            if (selectedObjects.Count != 1 && i < selectedObjects.Count)
            {
                unitList[i].gameObject.SetActive(true);
                unitList[i].transform.GetChild(0).GetComponent<RawImage>().texture = selectedObjects[i].thumbnail;
            }
            else
            {
                unitList[i].gameObject.SetActive(false);
            }
        }
    }

    /**********************************************************
     * UI의 명령 창 설정
     * 선택된 유닛 모두가 갖고 있는 명령만 출력한다. (같은 명령은 같은 위치에 정렬 필요)
     * 변수 controlable : 적군 및 중립 유닛은 다중 선택이 불가하므로, 선택된 첫 번째 유닛이 자신 소유일때만 true
     *********************************************************/
    public void SetCommandWithSelectedUnit()
    {
        bool controlable = selectedObjects.Count > 0 && IsMine(selectedObjects[0]);
        if (controlable)
        {
            // 첫 번째 유닛의 커맨드 정보 긁어옴
            Command[] commandList = selectedObjects[0].GetCommandList();
            // 다른 유닛의 커맨드와 일치하지 않는 커맨드는 제외
            //for(int i = 1; i < selectedObjects.Count; i++)
            //{
            //    Command[] otherCommandList = selectedObjects[i].GetCommandList();
            //    for (int j = 0; j < commandList.Length; j++)
            //    {
            //        if(!commandList[j].Equal(otherCommandList[j]))
            //        {
            //            commandList[j].info = Command.Info.None;
            //        }
            //    }
            //}

            for (int i = 0; i < commandImageList.Length; i++)
            {
                RawImage commandBg = commandImageList[i];
                RawImage commandIcon = commandImageList[i].transform.GetChild(0).GetComponent<RawImage>();
                Button commandButton = commandImageList[i].transform.GetChild(0).GetComponent<Button>();
                if (commandList[i].info == Command.Info.None) commandIcon.gameObject.SetActive(false);
                else
                {
                    commandIcon.gameObject.SetActive(true);
                    commandIcon.GetComponent<Button>().onClick.RemoveAllListeners();
                    if (commandList[i].info == Command.Info.Stop)
                    {
                        commandIcon.texture = stopTexture;
                        commandButton.onClick.AddListener(StopOrder);
                        commandBg.GetComponent<CommandTextBox>().SetText("Stop");
                    }
                    else if (commandList[i].info == Command.Info.Attack)
                    {
                        commandIcon.texture = attackTexture;
                        commandButton.onClick.AddListener(AttackOrder);
                        commandBg.GetComponent<CommandTextBox>().SetText("Attack");
                    }
                    else if (commandList[i].info == Command.Info.Hold)
                    {
                        commandIcon.texture = holdTexture;
                        commandButton.onClick.AddListener(HoldOrder);
                        commandBg.GetComponent<CommandTextBox>().SetText("Hold");
                    }
                    else if (commandList[i].info == Command.Info.Produce)
                    {
                        commandIcon.texture = commandList[i].unit.thumbnail;
                        for (int j = 0; j < selectedObjects.Count; j++)
                        {
                            if (IsMine(selectedObjects[j]) && selectedObjects[j].IsStructure())
                            {
                                Structure producer = (Structure)selectedObjects[j];
                                Unit producedUnit = commandList[i].unit;
                                commandButton.onClick.AddListener(() => producer.StartProduceSelectedUnit(producedUnit));
                            }
                        }
                        commandBg.GetComponent<CommandTextBox>().SetText
                            ("Produce " + commandList[i].unit.unitName + " (" + commandList[i].unit.shortcut + ")\n\nResource : " + commandList[i].unit.resource);
                    }
                    else if (commandList[i].info == Command.Info.Build)
                    {
                        commandIcon.texture = commandList[i].structure.thumbnail;
                        if (IsMine(selectedObjects[0]))
                        {
                            ObjectController builder = selectedObjects[0];
                            Structure builtStructure = commandList[i].structure;
                            commandButton.onClick.AddListener(() => ((Structure)builder).BuildSelectedStructure(builtStructure));
                        }
                        commandBg.GetComponent<CommandTextBox>().SetText
                            ("Build " + commandList[i].structure.unitName + " (" + commandList[i].structure.shortcut + ")\n\nResource : " + commandList[i].structure.resource);
                    }
                    //else if (commandList[i].info == Command.Info.Spell)
                    //{
                    //    commandIcon.texture = null;
                    //}
                }
            }
        }
        else
        {
            foreach (RawImage img in commandImageList)
            {
                img.GetComponent<CommandTextBox>().SetText("");
                img.transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    /**********************************************************
     * 새로 생성된 유닛을 미니맵에도 추가한다.
     *********************************************************/
    public void AddOnMiniMap(ObjectController target)
    {
        RawImage pointOnMiniMap = Instantiate(unitOnMinimap, minimap.transform.position, Quaternion.identity);
        pointOnMiniMap.transform.SetParent(minimap.transform);

        if (IsMine(target)) pointOnMiniMap.color = Color.green;
        else if (Global.Relation(target.owner, Global.playerName) == Team.Enemy) pointOnMiniMap.color = Color.red;
        else pointOnMiniMap.color = Color.yellow;

        onMiniMap.Add(pointOnMiniMap);
    }

    /**********************************************************
     * 죽은 유닛을 미니맵에서도 제거한다.
     *********************************************************/
    public void RemoveFromMiniMap(RawImage deleteImage)
    {
        if (onMiniMap.Contains(deleteImage))
        {
            onMiniMap.Remove(deleteImage);
            Destroy(deleteImage.gameObject);
        }
    }

    /**********************************************************
     * 미니맵 설정
     * 클래스 글로벌 변수 onMiniMap : 씬에 존재하는 모든 선택가능 오브젝트들의 미니맵 포인트
     * 선택가능 오브젝트들의 좌표를 미니맵 좌표로 치환하여 매핑되는 onMiniMap의 원소들 좌표를 수정한다.
     *********************************************************/
    public void SetMiniMap()
    {
        for (int i = 0; i < onMiniMap.Count; i++)
        {
            onMiniMap[i].transform.position = minimapController.GetMinimapPointFromWorldPoint(selectableObjects[i].transform.position);
        }
    }
    
    void PrintMessage(string message)
    {
        if (printMessageCoroutine != null) StopCoroutine(printMessageCoroutine);
        printMessageCoroutine = SetMessageBox(message);
        StartCoroutine(printMessageCoroutine);
    }

    IEnumerator SetMessageBox(string message)
    {
        messageBox.text = message;
        yield return new WaitForSeconds(1);
        messageBox.text = "";
    }

    /**********************************************************
     * 메인 카메라 전후좌우 이동
     * 카메라는 맵 밖을 비출 수 없다.
     * 카메라는 X축 각도를 이용, 이동 가능한 좌표 범위 계산
     *********************************************************/
    public void ControlMainCamera()
    {
        mCamera.transform.position = CameraPositionIntoMap(mCamera.transform.position + new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")));

        float allowableError = 0.01f;
        if (Input.mousePosition.x < Screen.width * allowableError) mCamera.transform.position = CameraPositionIntoMap(mCamera.transform.position + new Vector3(-1, 0, 0));
        if (Input.mousePosition.x > Screen.width * (1 - allowableError)) mCamera.transform.position = CameraPositionIntoMap(mCamera.transform.position + new Vector3(1, 0, 0));
        if (Input.mousePosition.y < Screen.height * allowableError) mCamera.transform.position = CameraPositionIntoMap(mCamera.transform.position + new Vector3(0, 0, -1));
        if (Input.mousePosition.y > Screen.height * (1 - allowableError)) mCamera.transform.position = CameraPositionIntoMap(mCamera.transform.position + new Vector3(0, 0, 1));
    }

    /**********************************************************
     * 카메라가 특정 지점을 중심으로 비추도록 하는 카메라 좌표 반환
     *********************************************************/
    public Vector3 GetCameraPosition(Vector3 point)
    {
        point.y = mCamera.transform.position.y;

        float degree = mCamera.transform.rotation.eulerAngles.x * Mathf.Deg2Rad;
        float zDiff = mCamera.transform.position.y / Mathf.Tan(degree) + Global.uiWidthHeightRadio * mCamera.orthographicSize / Mathf.Sin(degree);
        point.z -= zDiff;

        return CameraPositionIntoMap(point);
    }

    public void CameraToLookPoint(Vector3 point)
    {
        mCamera.transform.position = GetCameraPosition(point);
    }

    public Vector3 CameraPositionIntoMap(Vector3 point)
    {
        Vector3 mLossyScale = map.transform.lossyScale * Global.mapBaseLossyScale;
        float degree = mCamera.transform.rotation.eulerAngles.x * Mathf.Deg2Rad;

        float maxX = map.transform.position.x + mLossyScale.x / 2 - mCamera.orthographicSize * Screen.width / Screen.height;
        float minX = map.transform.position.x - mLossyScale.x / 2 + mCamera.orthographicSize * Screen.width / Screen.height;

        float maxZ = map.transform.position.z + mLossyScale.z / 2 - mCamera.transform.position.y / Mathf.Tan(degree) - mCamera.orthographicSize / Mathf.Sin(degree);
        float minZ = map.transform.position.z - mLossyScale.z / 2 - mCamera.transform.position.y / Mathf.Tan(degree) + mCamera.orthographicSize / Mathf.Sin(degree) * (1 - 2 * Global.uiWidthHeightRadio);

        float newX = point.x;
        float newZ = point.z;

        if (newX > maxX) newX = maxX;
        if (newX < minX) newX = minX;
        if (newZ > maxZ) newZ = maxZ;
        if (newZ < minZ) newZ = minZ;

        point.x = newX;
        point.z = newZ;

        return point;
    }
}