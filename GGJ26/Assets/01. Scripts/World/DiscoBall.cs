using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DiscoBall : MonoBehaviour
{
    [Header("이벤트 채널 설정")]
    [Tooltip("디스코볼 효과를 시작시키는 이벤트 채널입니다.")]
    public VoidEventChannelSO startDiscoEvent;
    [Tooltip("플레이어 연출이 끝난 후, 실제 종료를 시작시키는 '확정' 이벤트 채널입니다.")]
    public VoidEventChannelSO confirmStopDiscoEvent;

    [Header("라이트 설정")]
    public Light[] allSpotlights;
    public int numberOfGroups = 3;
    public Light centralPointLight;
    public Color[] discoColors;
    
    [Header("그룹 전환 설정")]
    public float groupActiveDuration = 5f;

    [Header("점멸 효과 설정")]
    public float minBlinkInterval = 0.5f;
    public float maxBlinkInterval = 1.0f;
    public float minOnTime = 0.3f;
    public float maxOnTime = 0.7f;

    [Header("실시간 회전 설정")]
    public float lightRotationSpeed = 0.5f;
    public Vector2 minMaxXRotation = new Vector2(45f, 90f);
    public bool randomizeYRotation = true;
    public float rotationSpeed = 30f;

    [Header("커튼 설정")]
    public Renderer[] curtainRenderers;

    [Header("스카이박스 설정")]
    [ColorUsage(false, true)] public Color discoSkyTint;

    [Header("태양 설정")]
    public Vector3 sunStartRotation;
    public Vector3 sunEndRotation;
    
    [Header("연출 설정")]
    [SerializeField] private GameObject discoObject; // 디스코볼 실제 오브젝트
    [SerializeField] private Light globalLight;
    [SerializeField] private float yOffset = 5f;
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private GameObject textCanvas;

    private List<List<Light>> _logicalLightGroups = new List<List<Light>>();
    private bool _isDiscoActive = false;
    private Vector3 _startPosition;
    private float _startIntensity;
    private Coroutine _discoLoopCoroutine;
    
    private float _originalSunSize;
    private Color _originalSkyTint;
    private Material _originalSkyboxMaterial;

    private void Awake()
    {
        if (discoObject != null)
        {
            _startPosition = discoObject.transform.position;
        }
        
        // Store original skybox material asset
        _originalSkyboxMaterial = RenderSettings.skybox;
        if (_originalSkyboxMaterial != null && _originalSkyboxMaterial.shader.name == "Skybox/Procedural")
        {
            _originalSunSize = _originalSkyboxMaterial.GetFloat("_SunSize");
            _originalSkyTint = _originalSkyboxMaterial.GetColor("_SkyTint");
        }

        if (globalLight == null) // If globalLight is not assigned in the Inspector
        {
            // Try to find a Light2D component in the scene first
            globalLight = GameObject.Find("GlobalLight").GetComponent<Light>();
            
            if (globalLight == null)
            {
                Debug.LogWarning("DiscoBall: GlobalLight was not automatically found in the scene.");
            }
        }
        
        if (globalLight != null) // Now check if globalLight is assigned (either manually or found)
        {
            _startIntensity = globalLight.intensity;
            globalLight.transform.rotation = Quaternion.Euler(sunStartRotation);
        }

        // 조명을 논리적 그룹으로 나누는 로직
        if (allSpotlights != null && allSpotlights.Length > 0)
        {
            if (numberOfGroups <= 0) numberOfGroups = 1;
            for (int i = 0; i < numberOfGroups; i++)
            {
                _logicalLightGroups.Add(new List<Light>());
            }
            for (int i = 0; i < allSpotlights.Length; i++)
            {
                _logicalLightGroups[i % numberOfGroups].Add(allSpotlights[i]);
            }
        }

        // Initially disable all lights
        if (allSpotlights != null)
        {
            foreach (var light in allSpotlights)
            {
                if(light != null)
                    light.enabled = false;
            }
        }
        if (centralPointLight != null)
        {
            centralPointLight.enabled = false;
        }

        discoObject.SetActive(false);

        if (curtainRenderers != null)
        {
            foreach (var renderer in curtainRenderers)
            {
                if(renderer != null)
                    renderer.gameObject.SetActive(false);
            }
        }
        
        textCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (startDiscoEvent != null) startDiscoEvent.OnEventRaised += StartDisco;
        if (confirmStopDiscoEvent != null) confirmStopDiscoEvent.OnEventRaised += StopDisco;
    }

    private void OnDisable()
    {
        if (startDiscoEvent != null) startDiscoEvent.OnEventRaised -= StartDisco;
        if (confirmStopDiscoEvent != null) confirmStopDiscoEvent.OnEventRaised -= StopDisco;
        
        StopAllCoroutines();
        // DOTween 시퀀스도 확실하게 중지
        DOTween.Kill(this);
    }

    void Update()
    {
        if (discoObject != null && discoObject.activeSelf)
        {
            discoObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    public void StartDisco()
    {
        if (_isDiscoActive || discoObject == null) return;
        
        StopAllCoroutines();
        StartCoroutine(StartDiscoSequence());
    }

    public void StopDisco()
    {
        if (!_isDiscoActive || discoObject == null) return;

        StopAllCoroutines();
        StartCoroutine(StopDiscoSequence());
    }

    private IEnumerator StartDiscoSequence()
    {
        _isDiscoActive = true;
        discoObject.SetActive(true);
        if (curtainRenderers != null)
        {
            foreach (var renderer in curtainRenderers)
            {
                if (renderer != null)
                    renderer.gameObject.SetActive(true);
            }
        }

        // Handle Skybox Material Instantiation
        Material currentSkyboxMaterial = _originalSkyboxMaterial;
        if (_originalSkyboxMaterial != null && _originalSkyboxMaterial.shader.name == "Skybox/Procedural")
        {
            // Create a temporary instance of the skybox material to modify
            currentSkyboxMaterial = new Material(_originalSkyboxMaterial);
            RenderSettings.skybox = currentSkyboxMaterial;
        }

        // Handle Global Light
        Sequence sequence = DOTween.Sequence();
        if (globalLight != null)
        {
            sequence.Join(globalLight.DOIntensity(0.1f, transitionDuration));
            sequence.Join(globalLight.transform.DORotate(sunEndRotation, transitionDuration));
        }
        sequence.Join(discoObject.transform.DOMoveY(_startPosition.y - yOffset, transitionDuration));
        
        // Handle Curtain Objects
        if (curtainRenderers != null && curtainRenderers.Length > 0)
        {
            sequence.Join(DOTween.To(() => 0f, x =>
            {
                foreach (var renderer in curtainRenderers)
                {
                    if (renderer != null)
                        renderer.material.SetFloat("_Progress", x);
                }
            }, 1f, transitionDuration));
        }
        
        // Handle Sun Position(Directional Light)
        if (currentSkyboxMaterial != null && currentSkyboxMaterial.shader.name == "Skybox/Procedural")
        {
            sequence.Join(currentSkyboxMaterial.DOFloat(0f, "_SunSize", transitionDuration));
            sequence.Join(currentSkyboxMaterial.DOColor(discoSkyTint, "_SkyTint", transitionDuration));
        }
        
        // Handle Text "Dancing Time" Objet
        textCanvas.SetActive(true);
        
        yield return sequence.WaitForCompletion();

        _discoLoopCoroutine = StartCoroutine(DiscoLoop());
    }

    private IEnumerator StopDiscoSequence()
    {
        _isDiscoActive = false;
        if (_discoLoopCoroutine != null)
        {
            StopCoroutine(_discoLoopCoroutine);
            _discoLoopCoroutine = null;
        }
        
        foreach (var group in _logicalLightGroups) TurnOffLightsInGroup(group);
        if (centralPointLight != null) centralPointLight.enabled = false;
        
        Sequence sequence = DOTween.Sequence();
        if (globalLight != null)
        {
            sequence.Join(globalLight.DOIntensity(_startIntensity, transitionDuration));
            sequence.Join(globalLight.transform.DORotate(sunStartRotation, transitionDuration));
        }
        sequence.Join(discoObject.transform.DOMoveY(_startPosition.y, transitionDuration));
        
        if (curtainRenderers != null && curtainRenderers.Length > 0)
        {
            sequence.Join(DOTween.To(() => 1f, x =>
            {
                foreach (var renderer in curtainRenderers)
                {
                    if(renderer != null)
                        renderer.material.SetFloat("_Progress", x);
                }
            }, 0f, transitionDuration));
        }
        
        // Animate the current skybox (which is the temporary one) back to original values
        if (RenderSettings.skybox != null && RenderSettings.skybox.shader.name == "Skybox/Procedural")
        {
            sequence.Join(RenderSettings.skybox.DOFloat(_originalSunSize, "_SunSize", transitionDuration));
            sequence.Join(RenderSettings.skybox.DOColor(_originalSkyTint, "_SkyTint", transitionDuration));
        }
        
        sequence.OnComplete(() =>
        {
            discoObject.SetActive(false);
            if (curtainRenderers != null)
            {
                foreach (var renderer in curtainRenderers)
                {
                    if(renderer != null)
                        renderer.gameObject.SetActive(false);
                }
            }
            
            // Restore original skybox material and destroy the temporary one
            if (RenderSettings.skybox != null && RenderSettings.skybox != _originalSkyboxMaterial)
            {
                Material tempMaterialToDestroy = RenderSettings.skybox; // Get reference to the temporary material
                RenderSettings.skybox = _originalSkyboxMaterial; // Restore original
                Destroy(tempMaterialToDestroy); // Destroy the temporary material
            }
        });
        
        textCanvas.SetActive(false);
        
        yield return sequence.WaitForCompletion();
    }
    
    IEnumerator DiscoLoop()
    {
        // --- 상태 및 타이머 변수 초기화 ---
        float groupSwitchTimer = 0f;
        float blinkTimer = 0f;
        float currentBlinkDuration = 0f;
        bool lightsAreOn = false;
        int currentGroupIndex = 0;
        
        var fromRotations = new Dictionary<Light, Quaternion>();
        var toRotations = new Dictionary<Light, Quaternion>();
        var lerpProgress = new Dictionary<Light, float>();

        List<Light> currentGroup = _logicalLightGroups.Count > 0 ? _logicalLightGroups[currentGroupIndex] : new List<Light>();
        InitializeRotationsForGroup(currentGroup, fromRotations, toRotations, lerpProgress);

        // --- 단일 마스터 루프 ---
        while (_isDiscoActive)
        {
            float deltaTime = Time.deltaTime;
            groupSwitchTimer += deltaTime;
            blinkTimer += deltaTime;

            if (groupSwitchTimer >= groupActiveDuration && _logicalLightGroups.Count > 1)
            {
                groupSwitchTimer = 0f;
                TurnOnLightsInGroup(currentGroup, false);

                currentGroupIndex = (currentGroupIndex + 1) % _logicalLightGroups.Count;
                currentGroup = _logicalLightGroups[currentGroupIndex];
                
                InitializeRotationsForGroup(currentGroup, fromRotations, toRotations, lerpProgress);
                lightsAreOn = false; 
                blinkTimer = 0f;
            }

            if (blinkTimer >= currentBlinkDuration)
            {
                blinkTimer = 0f;
                lightsAreOn = !lightsAreOn;

                TurnOnLightsInGroup(currentGroup, lightsAreOn);
                if (centralPointLight != null) centralPointLight.enabled = lightsAreOn;

                if (lightsAreOn)
                {
                    currentBlinkDuration = Random.Range(minOnTime, maxOnTime);
                    SetRandomColorsForGroup(currentGroup);
                    if (centralPointLight != null && discoColors.Length > 0)
                    {
                        centralPointLight.color = discoColors[Random.Range(0, discoColors.Length)];
                    }
                }
                else
                {
                    currentBlinkDuration = Random.Range(minBlinkInterval, maxBlinkInterval);
                }
            }
            
            UpdateLightRotations(currentGroup, fromRotations, toRotations, lerpProgress);
            
            yield return null;
        }
    }

    private void InitializeRotationsForGroup(List<Light> group, Dictionary<Light, Quaternion> from, Dictionary<Light, Quaternion> to, Dictionary<Light, float> progress)
    {
        from.Clear();
        to.Clear();
        progress.Clear();
        foreach(var light in group)
        {
            if(light == null) continue;
            from[light] = light.transform.localRotation;
            to[light] = GenerateRandomRotation();
            progress[light] = 0f;
        }
    }
    
    private void UpdateLightRotations(List<Light> lights, Dictionary<Light, Quaternion> from, Dictionary<Light, Quaternion> to, Dictionary<Light, float> progress)
    {
        foreach (var light in lights)
        {
            if (light == null || light.type != LightType.Spot) continue;

            progress[light] += Time.deltaTime * lightRotationSpeed;
            light.transform.localRotation = Quaternion.Slerp(from[light], to[light], progress[light]);

            if (progress[light] >= 1f)
            {
                from[light] = to[light];
                to[light] = GenerateRandomRotation();
                progress[light] = 0f;
            }
        }
    }

    private void SetRandomColorsForGroup(List<Light> groupLights)
    {
        bool hasColors = discoColors != null && discoColors.Length > 0;
        if (!hasColors) return;

        foreach (var light in groupLights)
        {
            if (light != null)
            {
                light.color = discoColors[Random.Range(0, discoColors.Length)];
            }
        }
    }

    private Quaternion GenerateRandomRotation()
    {
        float newX = Random.Range(minMaxXRotation.x, minMaxXRotation.y);
        float newY = randomizeYRotation ? Random.Range(0f, 360f) : 0;
        return Quaternion.Euler(newX, newY, 0);
    }

    private void TurnOffLightsInGroup(List<Light> groupLights)
    {
        TurnOnLightsInGroup(groupLights, false);
    }

    private void TurnOnLightsInGroup(List<Light> groupLights, bool state)
    {
        if (groupLights == null) return;
        foreach (Light light in groupLights)
        {
            if (light != null) light.enabled = state;
        }
    }
}