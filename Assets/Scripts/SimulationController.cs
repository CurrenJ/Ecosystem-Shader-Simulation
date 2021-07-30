using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComputeShaderUtility;

public class SimulationController : MonoBehaviour
{
    // Shader
    public ComputeShader computeShader;

    // Textures
    [SerializeField, HideInInspector] protected RenderTexture displayTexture;
    [SerializeField, HideInInspector] protected RenderTexture playerMap;
    [SerializeField, HideInInspector] protected RenderTexture terrainMap;
    private enum Kernel {append = 0, consume = 1, update = 2, draw = 3}

    // Display
    [SerializeField]
    private Vector2Int resolution;
    [SerializeField]
    private int maxPopulation;
    [SerializeField]
    private int livingPopulation;

    private ComputeBuffer organismBuffer;
    private ComputeBuffer organismCountBuffer;

    void Start()
    {
        Debug.Log(SystemInfo.graphicsDeviceName);
        Init();
    }

    private void LateUpdate()
    {
        RunSimulation();

        if (Input.GetKeyDown(KeyCode.E))
            AppendOrganisms();
        else if (Input.GetKeyDown(KeyCode.Q))
            ConsumeOrganisms();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RenderToScreen(dest);
    }

    private void Init()
    {
        ComputeHelper.CreateRenderTexture(ref displayTexture, resolution.x, resolution.y);
        ComputeHelper.CreateRenderTexture(ref playerMap, resolution.x, resolution.y);
        ComputeHelper.CreateRenderTexture(ref terrainMap, resolution.x, resolution.y);

        organismCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        CreateOrganisms();
        InitShaderParameters();
    }

    private void InitShaderParameters()
    {
        // Kernel 0 (Append) Parameters
        computeShader.SetTexture(((int)Kernel.append), "DisplayTexture", displayTexture);
        computeShader.SetTexture(((int)Kernel.append), "PlayerMap", playerMap);
        computeShader.SetTexture(((int)Kernel.append), "TerrainMap", terrainMap);

        // Kernel 1 (Consume) Parameters
        computeShader.SetTexture(((int)Kernel.consume), "DisplayTexture", displayTexture);
        computeShader.SetTexture(((int)Kernel.consume), "PlayerMap", playerMap);
        computeShader.SetTexture(((int)Kernel.consume), "TerrainMap", terrainMap);

        // Kernel 2 (Update)
        computeShader.SetTexture(((int)Kernel.update), "DisplayTexture", displayTexture);
        computeShader.SetTexture(((int)Kernel.update), "PlayerMap", playerMap);
        computeShader.SetTexture(((int)Kernel.update), "TerrainMap", terrainMap);

         // Kernel 3 (Draw)
        computeShader.SetTexture(((int)Kernel.draw), "DisplayTexture", displayTexture);
        computeShader.SetTexture(((int)Kernel.draw), "PlayerMap", playerMap);
        computeShader.SetTexture(((int)Kernel.draw), "TerrainMap", terrainMap);

        computeShader.SetInt("width", resolution.x);
        computeShader.SetInt("height", resolution.y);

        UpdateShaderParameters();
    }

    private void UpdateShaderParameters()
    {
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetFloat("time", Time.time);
        livingPopulation = GetBufferCounter(ref organismBuffer);
        computeShader.SetFloat("livingPopulation", livingPopulation);
    }

    // Render the result directly to the screen
    private void RenderToScreen(RenderTexture destination)
    {
        ComputeHelper.Dispatch(computeShader, resolution.x, resolution.y, 1, 3);
        Graphics.Blit(displayTexture, destination);
    }

    // Multi-step sim update
    private void RunSimulation(int steps)
    {
        for (int s = 0; s < steps; s++)
        {
            RunSimulation();
        }
    }

    // Single-step sim update
    private void RunSimulation()
    {
        ComputeHelper.ClearRenderTexture(playerMap);
        ComputeHelper.ClearRenderTexture(displayTexture);

        UpdateShaderParameters();

        if (livingPopulation > 0)
            ComputeHelper.Dispatch(computeShader, livingPopulation, 1, 1, 2);
    }

    struct Organism
    {
        public float speciesId;
        // SPECIES COLOR FLOAT4 WILL BE A CONSTANT IN SHADER, no need for every organism to store

        public Vector2 position;
        public float angle;

        public float movementSpeed;
        public float turnSpeed;

        public float herdingFactor;

        public float foodLevel;
        public float waterLevel;
        public float visionRadius;
    };

    private void CreateOrganisms()
    {
        Organism[] organisms = new Organism[maxPopulation];
        for (int i = 0; i < maxPopulation; i++)
        {
            int t_speciesId = 0;

            Organism organism = new Organism()
            {
                speciesId = t_speciesId,
                position = new Vector2(UnityEngine.Random.Range(0, resolution.x), UnityEngine.Random.Range(0, resolution.y)),
                angle = UnityEngine.Random.Range(0, 2 * Mathf.PI),
                movementSpeed = 1,
                turnSpeed = 1,
                herdingFactor = 0,
                foodLevel = 1,
                waterLevel = 1,
                visionRadius = 1
            };
            //organisms[i] = organism;
        }

        // Append buffer
        organismBuffer = new ComputeBuffer(maxPopulation, ComputeHelper.GetStride<Organism>(), ComputeBufferType.Append);
        organismBuffer.SetCounterValue(0);
        organismBuffer.SetData(organisms);
        computeShader.SetBuffer(0, "organismsAppend", organismBuffer);
        computeShader.SetBuffer(1, "organismsConsume", organismBuffer);
        computeShader.SetBuffer(2, "organismsRead", organismBuffer);

        computeShader.SetInt("maxPopulation", maxPopulation);
        computeShader.SetInt("livingPopulation", 0);

        print(organisms[0].position);
        PrintOrganismBufferCounter();
    }

    private void PrintOrganismBufferCounter()
    {
        int[] organismCountAppendBuffer = new int[1];
        organismCountBuffer.SetData(organismCountAppendBuffer);
        ComputeBuffer.CopyCount(organismBuffer, organismCountBuffer, 0);
        organismCountBuffer.GetData(organismCountAppendBuffer);
        Debug.Log(organismCountAppendBuffer[0]);
        Organism[] orgs = new Organism[maxPopulation];
        organismBuffer.GetData(orgs);
        PrintOrganismArrayData(ref orgs);
    }

    private int GetBufferCounter(ref ComputeBuffer buffer)
    {

        // Setup temp buffer
        int[] counterBuffer = new int[1];
        ComputeBuffer tempBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        tempBuffer.SetData(counterBuffer);

        // Move buffer count into temp buffer
        ComputeBuffer.CopyCount(buffer, tempBuffer, 0);
        // Get counter from tempBuffer and store it
        tempBuffer.GetData(counterBuffer);
        // Release buffer
        tempBuffer.Release();
        return counterBuffer[0];
    }

    private void PrintOrganismArrayData(ref Organism[] arr)
    {
        string str = "[";
        for (int i = 0; i < arr.Length; i++)
        {
            if (i != arr.Length - 1)
            {
                // str += "(" + Mathf.CeilToInt(arr[i].position.x) + ", " + Mathf.CeilToInt(arr[i].position.y) + ")" + ", ";
                str += "(" + Mathf.CeilToInt(arr[i].speciesId * 100) + ")" + ", ";
            }
            else
            {
                // str += "(" + Mathf.CeilToInt(arr[i].position.x) + ", " + Mathf.CeilToInt(arr[i].position.y) + ")";
                str += "(" + Mathf.CeilToInt(arr[i].speciesId * 100) + ")";
            }
        }
        str += "]";
        Debug.Log(str);
    }

    private void OnDestroy()
    {
        PrintOrganismBufferCounter();
        ReleaseBuffers();
    }

    private void ReleaseBuffers()
    {
        organismBuffer.Release();
        organismCountBuffer.Release();
    }

    private void AppendOrganisms()
    {
        if (GetBufferCounter(ref organismBuffer) + maxPopulation / 10 <= maxPopulation)
        {
            // Append
            // Amount to append is set inside shader (and here. should be the same in both places)
            ComputeHelper.Dispatch(computeShader, 100, 1, 1, 0);
            PrintOrganismBufferCounter();
        }
        else
        {
            Debug.Log("Failed to consume: counter above population size.");
        }
    }

    private void ConsumeOrganisms()
    {
        if (GetBufferCounter(ref organismBuffer) - maxPopulation / 10 >= 0)
        {
            // Consume
            // Amount to consume is set inside shader (and here. should be the same in both places)
            ComputeHelper.Dispatch(computeShader, 100, 1, 1, 1);
            PrintOrganismBufferCounter();
        }
        else
        {
            Debug.Log("Failed to consume: counter below zero.");
        }
    }
}
