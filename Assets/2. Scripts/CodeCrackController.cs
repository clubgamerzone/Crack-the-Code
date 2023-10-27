using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

[Serializable]
public class Results
{
    public TextMeshProUGUI Time;
    public TextMeshProUGUI Result;
}
public class CodeCrackController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timer;
    [SerializeField] private TMP_InputField _codeToCrack;
    [SerializeField] private TextMeshProUGUI _currentCodeTry;
    [SerializeField] private Button _startCrackingCode;
    [SerializeField] private Button _startCrackingCodeCoroutine;
    [SerializeField] private Button _startCrackingCodeMultiCoroutine;
    [SerializeField] private Button _startCrackingCodeWithJob;
    [SerializeField] private Button _startCrackingCodeWithJobParallel;
    [SerializeField] private Button _startCrackingCodeWithTask;
    [SerializeField] private Button _clear;
    private long _code = 0;
    [SerializeField] private TextMeshProUGUI _correctCodeResult;
    private Stopwatch stopwatch = new Stopwatch();
    private long _correctNumber = -1;
    public bool ShouldUpdateTMPro;
    public bool EnableSampling;

    // Start is called before the first frame update
    [SerializeField] private List<Results> Results = new List<Results>();
    void Start()
    {
        foreach (Results result in Results)
        {
            result.Time.text = "";
            result.Result.text = "";
        }

        _startCrackingCode.onClick.AddListener(OnStartCrackingCode);
        _startCrackingCodeWithJob.onClick.AddListener(StartCrackingCodeWithJob);
        _startCrackingCodeWithJobParallel.onClick.AddListener(StartCrackingCodeWithJobParallel);
        _startCrackingCodeWithTask.onClick.AddListener(StartCrackingWithTask);
        _startCrackingCodeCoroutine.onClick.AddListener(StartCrackingCodeCoroutine);
        _startCrackingCodeMultiCoroutine.onClick.AddListener(StartCrackingCodeMultiCoroutine);
        _clear.onClick.AddListener(OnClear);
    }
    private void OnClear()
    {
        _correctCodeResult.text = "";
        _currentCodeTry.text = "";
    }

    private long CrackTheCode()
    {
        _code = long.Parse(_codeToCrack.text);
        _correctNumber = -1;

        for (long i = 0; i <= _code + 1; i++)
        {
            if (ShouldUpdateTMPro)
                _currentCodeTry.text = i.ToString();

            if (i == _code)
            {
                _correctNumber = i;
                break;
            }
        }

        return _correctNumber;
    }
    private void OnStartCrackingCode()
    {
        if (EnableSampling)
            Profiler.BeginSample("CrackTheCode");

        stopwatch.Reset();
        stopwatch.Start();

        long result = CrackTheCode();

        stopwatch.Stop();

        _correctCodeResult.text = "The correct number is: " + result + "\nTime taken (ms): " + stopwatch.ElapsedMilliseconds;

        if (EnableSampling)
            Profiler.EndSample();

        _startCrackingCodeWithTask.gameObject.SetActive(true);
        Results[0].Time.text = stopwatch.ElapsedMilliseconds.ToString();
        Results[0].Result.text = result.ToString();
    }

    #region Coroutine
    private void StartCrackingCodeCoroutine()
    {
        StartCoroutine(CrackCO());
    }

    private IEnumerator CrackCO()
    {
        stopwatch.Reset();
        stopwatch.Start();
        _correctNumber = -1;
        _code = long.Parse(_codeToCrack.text);

        for (long i = 0; i <= _code + 1; i++)
        {
            if (ShouldUpdateTMPro)
                _currentCodeTry.text = i.ToString();

            if (i == _code)
            {
                _correctNumber = i;
                break;
            }
        }
        yield return new WaitUntil(() => _correctNumber != -1);
        long result = _correctNumber;

        stopwatch.Stop();

        _correctCodeResult.text = "The correct number is: " + result + "\nTime taken (ms): " + stopwatch.ElapsedMilliseconds;
        _startCrackingCodeWithTask.gameObject.SetActive(true);
        Results[1].Time.text = stopwatch.ElapsedMilliseconds.ToString();
        Results[1].Result.text = result.ToString();
    }
    #endregion

    #region Multicoroutine
    private void StartCrackingCodeMultiCoroutine()
    {
        _code = long.Parse(_codeToCrack.text);
        _correctNumber = -1;

        stopwatch.Reset();
        stopwatch.Start();

        int numWorkers = 4;

        int range = (int)_code + 1;
        int chunkSize = range / numWorkers;
        int startIndex;
        int endIndex;

        for (int i = 0; i < numWorkers; i++)
        {
            startIndex = i * chunkSize;
            endIndex = startIndex + chunkSize;
            StartCoroutine(MultipleCoroutines(startIndex, endIndex));
        }
    }
    private IEnumerator MultipleCoroutines(int start, int end)
    {
        Debug.Log(start + " " + end);
        for (long i = start; i <= end; i++)
        {
            if (ShouldUpdateTMPro)
                _currentCodeTry.text = i.ToString();

            if (i == _code)
            {
                _correctNumber = i;
                break;
            }
        }
        yield return null;
        if (_correctNumber != -1)
        {
            long result = _correctNumber;

            stopwatch.Stop();

            _correctCodeResult.text = "The correct number is: " + result + "\nTime taken (ms): " + stopwatch.ElapsedMilliseconds;
            _startCrackingCodeWithTask.gameObject.SetActive(true);
            Results[2].Time.text = stopwatch.ElapsedMilliseconds.ToString();
            Results[2].Result.text = result.ToString();
        }

    }
    #endregion

    #region Task
    private async void StartCrackingWithTask()
    {
        _correctNumber = -1;
        _code = long.Parse(_codeToCrack.text);

        Debug.Log("task pressed");
        if (EnableSampling)
            Profiler.BeginSample("task");
        ShouldUpdateTMPro = false;
        stopwatch.Reset();
        stopwatch.Start();
        if (EnableSampling)
            Profiler.EndSample();
        long result = await Task.Run(() => CrackTheCode());

        stopwatch.Stop();

        _correctCodeResult.text = "The correct number is: " + result + "\nTime taken (ms): " + stopwatch.ElapsedMilliseconds;
        _startCrackingCodeWithJob.gameObject.SetActive(true);
        Results[3].Time.text = stopwatch.ElapsedMilliseconds.ToString();
        Results[3].Result.text = result.ToString();

    }
    #endregion

    #region Job

    [BurstCompile]
    struct CrackCodeJob : IJob
    {
        public long codeToCrack;
        public NativeArray<long> results;
        public void Execute()
        {
            for (long i = 0; i <= codeToCrack + 1; i++)
            {
                if (i == codeToCrack)
                {
                    results[0] = i;
                    return;
                }
            }
            results[0] = -1;
        }
    }
    private void StartCrackingCodeWithJob()
    {
        _code = long.Parse(_codeToCrack.text);

        Profiler.BeginSample("StartCrackingCodeWithJob");

        stopwatch.Reset();
        stopwatch.Start();

        NativeArray<long> results = new NativeArray<long>(1, Allocator.TempJob);

        CrackCodeJob job = new CrackCodeJob
        {
            codeToCrack = _code,
            results = results
        };

        JobHandle jobHandle = job.Schedule();

        jobHandle.Complete();

        long correctNumber = results[0];

        results.Dispose();

        stopwatch.Stop();

        _correctCodeResult.text = "The correct number is: " + correctNumber + "\nTime taken (ms): " + stopwatch.ElapsedMilliseconds;

        _startCrackingCodeWithJobParallel.gameObject.SetActive(true);
        Profiler.EndSample();
        Results[4].Time.text = stopwatch.ElapsedMilliseconds.ToString();
        Results[4].Result.text = correctNumber.ToString();

    }

    #endregion

    #region ParallelFor
    [BurstCompile]
    struct ParallelCrackCodeJob : IJobParallelFor
    {
        public long codeToCrack;
        public NativeArray<long> results;

        public void Execute(int index)
        {
            int numWorkers = results.Length;

            // Calculate the range for this worker
            long range = codeToCrack + 1;
            long chunkSize = range / numWorkers;
            long startIndex = index * chunkSize;
            long endIndex = (index == numWorkers - 1) ? range : startIndex + chunkSize;

            for (long i = startIndex; i < endIndex; i++)
            {
                if (i == codeToCrack)
                {
                    results[index] = i;
                    return;
                }
            }

            results[index] = -1;
        }
    }

    private void StartCrackingCodeWithJobParallel()
    {
        _code = long.Parse(_codeToCrack.text);
        Profiler.BeginSample("StartCrackingCodeWithJobParallel");
        stopwatch.Reset();
        stopwatch.Start();

        int numWorkers = JobsUtility.MaxJobThreadCount;
        NativeArray<long> results = new NativeArray<long>(numWorkers, Allocator.TempJob);

        ParallelCrackCodeJob job = new ParallelCrackCodeJob
        {
            codeToCrack = _code,
            results = results
        };

        JobHandle jobHandle = job.Schedule(numWorkers, 1);

        jobHandle.Complete();

        long correctNumber = -1;
        for (int i = 0; i < numWorkers; i++)
        {
            if (results[i] != -1)
            {
                correctNumber = results[i];
                break;
            }
        }


        results.Dispose();

        stopwatch.Stop();

        _correctCodeResult.text = "The correct number is: " + correctNumber + "\nTime taken (ms): " + stopwatch.ElapsedMilliseconds;

        Profiler.EndSample();
        Results[5].Time.text = stopwatch.ElapsedMilliseconds.ToString();
        Results[5].Result.text = correctNumber.ToString();
    }

    #endregion
}
