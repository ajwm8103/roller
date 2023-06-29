using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class RollerManager : MonoBehaviour
{
    public static RollerManager Instance;

    private void Awake()
    {
        if (Instance == null){
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    List<Roller> rollers;

    List<Tuple<Roller, RollerGenotype>> currentGeneration;
    List<RollerGenotype> evaluatedGenotypes;

    public int numGenes = 10;
    public int generation = 0;
    public GameObject environmentPrefab;
    public float heightDiff = 20f;
    public int envCount = 20;
    public float maxTime = 10f;
    
    // Start is called before the first frame update
    void Start()
    {
        rollers = new List<Roller>();
        evaluatedGenotypes = new List<RollerGenotype>();
        currentGeneration = new List<Tuple<Roller, RollerGenotype>>();

        // Generate environments
        for (int i = 1; i < envCount + 1; i++)
        {
            GameObject environment = Instantiate(environmentPrefab, new Vector3(0, i * heightDiff, 0), Quaternion.identity);
            environment.transform.parent = transform;
            Roller roller = environment.transform.GetComponentInChildren<Roller>();

            Debug.Log(roller == null);
            rollers.Add(roller);

            RollerGenotype rollerGenotype = GenerateRandomRoller();
            roller.InitializeGenotype(rollerGenotype);

            currentGeneration.Add(new Tuple<Roller, RollerGenotype>(roller, rollerGenotype));
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if all rollers have ended (currentGeneration empty)
        if (currentGeneration.Count == 0)
        {
            CreateNewGeneration();
        }
    }

    public RollerGenotype GenerateRandomRoller()
    {
        RollerGenotype genotype = new RollerGenotype(numGenes);

        // Mutate each gene between 0.5f and 1.5f
        for (int i = 0; i < numGenes; i++)
        {
            genotype.genes[i] = UnityEngine.Random.Range(0.5f, 1.5f);
        }
        return genotype;
    }

    public void OnRollerEnded(Roller roller){
        // Find and remove roller from currentGeneration
        Tuple<Roller, RollerGenotype> rollerTuple = currentGeneration.Find(x => x.Item1 == roller);
        currentGeneration.Remove(rollerTuple);
        evaluatedGenotypes.Add(rollerTuple.Item2);
    }

    public void CreateNewGeneration(){
        // Select top 1/5 of genotypes
        List<RollerGenotype> topEvals = SelectTopEvals();

        // Mutate and crossover to create new genotypes
        List<RollerGenotype> newRollerGenotypes = FromMutation(envCount, topEvals);

        // Initialize rollers with new genotypes
        for (int i = 0; i < envCount; i++)
        {
            Roller roller = rollers[i];
            RollerGenotype rollerGenotype = newRollerGenotypes[i];
            roller.InitializeGenotype(rollerGenotype);
            currentGeneration.Add(new Tuple<Roller, RollerGenotype>(roller, rollerGenotype));
            roller.Reset();
        }

    }

    private List<RollerGenotype> SelectTopEvals()
    {
        List<RollerGenotype> cleanedEvals = new List<RollerGenotype>(evaluatedGenotypes);
        List<RollerGenotype> topEvals = new List<RollerGenotype>();
        List<RollerGenotype> sortedEvals = cleanedEvals.OrderByDescending(x => x.fitness).ToList();

        int topCount = Mathf.RoundToInt(envCount * 0.2f);
        for (int i = 0; i < topCount; i++)
        {
            RollerGenotype eval = sortedEvals[i];
            topEvals.Add(eval);
        }

        //CreatureGenotypeEval bestEval = GetBestCreatureEval();
        //Debug.Log("Best: " + topEvals.Max(x => x.fitness.Value));
        //saveK.best = bestEval;
        return topEvals;
    }

    public List<RollerGenotype> FromMutation(int size, List<RollerGenotype> topEvals)
    {
        List<RollerGenotype> output = new List<RollerGenotype>();

        int topCount = topEvals.Count;
        int remainingCount = size - topEvals.Count;

        List<RollerGenotype> topSoftmaxEvals = new List<RollerGenotype>();

        float minFitness = topEvals.Min(x => (float)x.fitness);
        float maxFitness = topEvals.Max(x => (float)x.fitness);
        float scalingFactor = (maxFitness != minFitness) ? 1.0f / (maxFitness - minFitness) : 1.0f;

        // float exponent = 0.5f;
        float temperature = 0.1f; // You can adjust this value to find the right balance
        float denom = topEvals.Select(x => Mathf.Pow((float)(x.fitness - minFitness) * scalingFactor, 1 / temperature)).Sum();
        //float denom = topEvals.Select(x => Mathf.Exp((float)x.fitness.Value - maxFitness)).Sum();
        topEvals = topEvals.OrderByDescending(x => x.fitness).ToList();

        //Debug.Log(string.Format("[{0}, {1}], scaling factor {2}, denom {3}", maxFitness, minFitness, scalingFactor, denom));
        foreach (RollerGenotype topEval in topEvals)
        {
            // Replaces original 1/5th
            output.Add(new RollerGenotype(topEval));
            RollerGenotype topSoftmaxEval = topEval;
            //topSoftmaxEval.fitness = Mathf.Exp((float)topSoftmaxEval.fitness.Value - maxFitness);
            //topSoftmaxEval.fitness = Mathf.Pow((float)topSoftmaxEval.fitness.Value, exponent);
            topSoftmaxEval.fitness = Mathf.Pow((float)(topSoftmaxEval.fitness - minFitness) * scalingFactor, 1 / temperature);
            topSoftmaxEvals.Add(topSoftmaxEval);
        }

        topSoftmaxEvals = topSoftmaxEvals.OrderByDescending(x => x.fitness).ToList();

        List<int> intValues = new List<int>();

        // Calculate the integer values for each percentage
        int sizeChildren = size - topEvals.Count;
        foreach (RollerGenotype topSoftmaxEval in topSoftmaxEvals)
        {
            intValues.Add((int)(sizeChildren * (float)topSoftmaxEval.fitness / denom));
        }

        // Calculate the difference between the sum of the integer values and the desired size
        int diff = sizeChildren - intValues.Sum();

        // Distribute the difference across the integer values
        for (int i = 0; i < diff; i++)
        {
            intValues[i % intValues.Count]++;
        }

        for (int i = 0; i < intValues.Count; i++)
        {
            RollerGenotype cg = topSoftmaxEvals[i];
            int childrenCount = intValues[i];
            //if (i <= 1) Debug.Log(string.Format("{0} ({1}, {2}), children; {3}/{4}", cg.name, topEvals[i].fitness.Value, topSoftmaxEvals[i].fitness.Value / denom, childrenCount, sizeChildren));
            for (int j = 0; j < childrenCount; j++)
            {
                output.Add(new RollerGenotype(Mutate(cg)));
            }
        }

        return output;
    }

    public RollerGenotype Mutate(RollerGenotype g){
        // Mutate genes
        for (int i = 0; i < numGenes; i++)
        {
            if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.1f)
            {
                g.genes[i] = UnityEngine.Random.Range(0.5f, 1.5f);
            }
        }
        return g;
    }
}
