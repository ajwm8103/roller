using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RollerGenotype
{
    public float[] genes;
    public int numGenes { get { return genes.Length; } }
    public float fitness = 0f;

    public RollerGenotype(int numGenes)
    {
        genes = new float[numGenes];
    }

    public RollerGenotype(RollerGenotype oldGeno){
        genes = oldGeno.genes;
        fitness = 0f;
    }
}

public class Roller : MonoBehaviour
{
    Rigidbody2D myRigidbody;
    PolygonCollider2D polygonCollider2D;

    private Vector3 startPos;
    private Quaternion startRotation;
    public RollerGenotype genotype;
    public bool ended = false;
    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float distanceCovered = 0f;

    // Start is called before the first frame update
    void Awake()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>();
        myRigidbody = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        startRotation = transform.rotation;
    }

    public void Reset()
    {
        transform.position = startPos;
        transform.rotation = startRotation;
    }

    public void InitializeGenotype(RollerGenotype genotype)
    {
        this.genotype = genotype;
        Vector2[] points = CreateNGon(genotype.numGenes, 1f);

        for (int i = 0; i < genotype.numGenes; i++)
        {
            points[i] *= genotype.genes[i];
        }
        polygonCollider2D.points = points;
    }

    // Detect collision with Target layer
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Target"))
        {
            ended = true;
            RollerManager.Instance.OnRollerEnded(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        myRigidbody.angularVelocity = -180f;

        // Check if out of time
        timeElapsed += Time.deltaTime;
        if (timeElapsed > RollerManager.Instance.maxTime)
        {
            ended = true;
            RollerManager.Instance.OnRollerEnded(this);
        }
        // InitializeGenotype(genotype);
    }

    

    private Vector2[] CreateNGon(int sides, float radius)
    {
        Vector2[] points = new Vector2[sides];
        float angleStep = 2 * Mathf.PI / sides;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * angleStep;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

}
