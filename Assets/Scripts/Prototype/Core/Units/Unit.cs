using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(MaterialSetter))]
[RequireComponent(typeof(IObjectTweener))]
public abstract class Unit : MonoBehaviour
{
    // Dependencies
    private MaterialSetter materialSetter;
    public IObjectTweener tweener;

    [Header("Properties (read-only)")]
    // Only for display in Unity Editor. DO NOT USE.
    [SerializeField] private Vector2Int SF_unitPosition;
    [SerializeField] private PlayerTeam SF_unitTeam;
    [SerializeField] private int SF_availableMoveCount = 0;
    [SerializeField] private int SF_availableAttackCount = 0;
    [SerializeField] private int SF_id = 0;
    [SerializeField] private int SF_ActionCount = 0;
    [SerializeField] private int SF_currentHP = 1;
    

    [Header("Stats")]
    // To edit the stats, edit the prefab of the unit
    // These are default values
    [SerializeField] public int maxHP = 1;
    [SerializeField] public int ATK = 1;
    [SerializeField] public int DEF = 1;
    protected int HP = 1;

    [Header("Particles")]
    [SerializeField] protected GameObject FXHit;

    [Header("HealthBar")]
    //connects the healthbar script to the units
    public HealthBarScript healthbar;
    public HealthBarScriptCurer healthbarC;
    public HealthBarScriptQueen healthbarQ;

    // Important properties
    private int id = 0;
    public Field Field { get; set; }
    public Vector2Int OccupiedSquare { get; set; }
    public PlayerTeam Team { get; set; }
    public bool HasMoved { get; set; }
    public int ActionCount { get; set; }

    // Available move set
    public HashSet<Vector2Int> availableMoves;
    // Available attack set
    public HashSet<Vector2Int> availableAttacks;

    // Abstract methods. Implement these in inherited Units
    #region Abstract
    // Generate available move set
    public abstract HashSet<Vector2Int> GenerateAvailableMoves();
    // Generate available attack set
    public abstract HashSet<Vector2Int> GenerateAvailableAttacks();
    #endregion

    // ID system
    #region ID system
    private static int nextID = 1;
    protected int InitializeID()
    {
        if (id > 0)
            return id;
        else
            return (id = nextID++);
    }
    #endregion

    // Initialize and Update methods
    #region Initialize and Update
    private void Awake()
    {
        // Set dependencies
        materialSetter = GetComponent<MaterialSetter>();
        tweener = GetComponent<IObjectTweener>();

        // Initialize properties
        HasMoved = false;
        availableMoves = new HashSet<Vector2Int>();
        availableAttacks = new HashSet<Vector2Int>();

        // Initialize stats
        HP = maxHP;
        if (healthbar)
            healthbar.setMaxHealth(maxHP);
        if (healthbarC)
            healthbarC.setMaxHealthC(maxHP);
        if (healthbarQ)
            healthbarQ.setMaxHealthQ(maxHP);
    }

    private void Update()
    {
        // Update display values to internal values
        // Only for debugging
        SF_unitPosition = OccupiedSquare;
        SF_unitTeam = Team;
        SF_availableMoveCount = availableMoves.Count;
        SF_availableAttackCount = availableAttacks.Count;
        SF_id = id;
        SF_ActionCount = ActionCount;
        SF_currentHP = HP;
    }

    // Set data after instantitation
    public void SetData(Vector2Int coords, PlayerTeam team, Field field)
    {
        // ID system
        InitializeID();

        // Important properties
        this.OccupiedSquare = coords;
        this.Team = team;
        this.Field = field;

        // GameObject position
        transform.position = field.CalculatePositionFromCoords(coords);

        // Temporary solution
        // Hard-code the fix to the prefabs
        // TODO: Put this inside the child classes!
        if (this is XQueen)
        {
            transform.position = transform.position + new Vector3(0, -2.5f, 0);
            transform.Rotate(new Vector3(0, 270, 0));
        }
        if (this is XDrone) {
            transform.Rotate(new Vector3(0, 180, 0));
        }
        if (this is XWarrior) {
            transform.Rotate(new Vector3(0, 180, 0));
        }
        if (this is Curer) {
            //transform.position = transform.position + new Vector3(0, 2f, 2.5f);
            if (this.Team == PlayerTeam.P1)
                transform.Translate(new Vector3(1f, -2.3f, 2.5f), Space.World);
            else if (this.Team == PlayerTeam.P2)
                transform.Translate(new Vector3(-1f, -2.3f, -2.5f), Space.World);
            transform.Rotate(new Vector3(0, 180, 0));
        }
        if (this is Destroyer) {
            transform.Rotate(new Vector3(0, 180, 0));
            transform.Translate(new Vector3(0f, 0.7f, 0f), Space.World);
        }

        // Post-hard-code prefab should align with P1. Rotate y180 for P2 alignment
        if (this.Team == PlayerTeam.P2)
            transform.Rotate(new Vector3(0, 180, 0));
    }
    #endregion

    // Get methods
    #region Getters
    public int getHP()
    {
        return HP;
    }

    public int getATK()
    {
        return ATK;
    }

    public int getDEF()
    {
        return DEF;
    }
    #endregion

    // Logic methods
    // Important for other methods to use
    #region Logic
    public bool IsFromSameTeam(Unit unit)
    {
        return Team == unit.Team;
    }

    public bool CanMoveTo(Vector2Int coords)
    {
        // If the coords are not in the available move set, return false
        if (!availableMoves.Contains(coords))
            return false;
        // If there is a unit on that coords, return false
        Unit unit = Field.GetUnitOnSquare(coords);
        if (unit)
            return false;
        // Else, return true
        return true;
    }

    public bool CanAttackAt(Vector2Int coords)
    {
        // If the coords are not in the available attack set, return false
        if (!availableAttacks.Contains(coords))
            return false;
        // If there is a unit on that square, continue
        Unit unit = Field.GetUnitOnSquare(coords);
        if (unit)
        {
            // If that unit is from the same team, return false
            if (unit.IsFromSameTeam(this))
                return false;
            // Else, return true
            return true;
        }
        // If there is no unit on that square, return false
        return false;
    }

    public bool IsAlive()
    {
        return HP > 0;
    }

    public virtual void Die(Unit source)
    {
        // Debug:
        Debug.Log("[!] " + this + " has died.");

        Field.ShowExecutionAnimation(source, this);
        Field.RemoveUnit(this);
    }
    
    public bool HasAction()
    {
        return ActionCount > 0;
    }
    #endregion

    // Turn system
    #region Turn system
    public virtual void StartTurn()
    {
        ActionCount = 1;
    }

    public void GenerateActions()
    {
        ClearMoves();
        ClearAttacks();
        
        if (HasAction())
        {
            GenerateAvailableMoves();
            GenerateAvailableAttacks();
        }
    }

    public virtual void EndAction()
    {
        ActionCount--;
    }

    public virtual void EndTurn()
    {
        ActionCount = 0;
        SoundEffects.Instance.PlayButton();
    }
    #endregion

    // Movement methods
    #region Movement
    public virtual void MoveUnit(Vector2Int coords)
    {
        Vector3 targetPosition = Field.CalculatePositionFromCoords(coords);

        // [] Temporary solution
        targetPosition.y = transform.position.y;

        OccupiedSquare = coords;
        HasMoved = true;

        tweener.MoveTo(transform, targetPosition);

        SoundEffects.Instance.PlayMovement();
    }

    protected void ClearMoves()
    {
        availableMoves.Clear();
    }

    protected void AddMove(Vector2Int coords)
    {
        availableMoves.Add(coords);
    }

    protected void AddMoves(ICollection<Vector2Int> coordsCollection)
    {
        availableMoves.UnionWith(coordsCollection);
    }

    protected void RemoveMove(Vector2Int coords)
    {
        availableMoves.Remove(coords);
    }

    protected void RemoveMoves(ICollection<Vector2Int> coordsCollection)
    {
        availableMoves.ExceptWith(coordsCollection);
    }

    #endregion

    // Attack methods
    // Also include Damage() and Heal()
    #region Attack
    public virtual bool AttackAt(Vector2Int coords)
    {
        Unit enemy = Field.GetUnitOnSquare(coords);
        // If there's no enemy, return false
        if (enemy == null)
            return false;
        // If enemy is from same team, return false
        if (enemy.IsFromSameTeam(this))
            return false;

        // Particle system:
        if (FXHit != null)
        {
            GameObject particle = Instantiate(FXHit);
            particle.transform.position = transform.position;
            Destroy(particle, 5.0f);
        }

        // Damage increase from this Char's ATK roll
        int damageAddition = this.ATK;
        // Damage decrease from enemy's DEF roll
        int damageReduction = enemy.DEF;

        // Gate damage to be min 1
        int dmg = Math.Max(damageAddition - damageReduction, 1);

        // Debug:
        Debug.Log("[-] " + this + " attacked " + enemy + ", dealing " + dmg + " damage.");

        // Damage the enemy
        enemy.Damage(dmg, this);

        enemy.PlayDamageSound();
        return true;
    }

    protected virtual void PlayDamageSound()
    {
        SoundEffects.Instance.PlayHit();
    }

    public virtual void Damage(int dmg, Unit source)
    {
        // (source) is not needed for now, but it's good to have in future implementations
        // If this Char is not alive or the damage is not positive, return
        if (!IsAlive() || dmg < 0)
            return;

        // Reduce HP by damage amount
        HP -= dmg;

        // Debug:
        Debug.Log("[-] " + this + " has been damaged, HP before gate: " + this.HP);

        // Gate HP to be min 0
        if (this.HP < 0)
            HP = 0;

        // HP bar update
        if (healthbar)
            healthbar.setHealth(HP);
        else if (healthbarC)
            healthbarC.setHealthC(HP);
        else if (healthbarQ)
            healthbarQ.setHealthQ(HP);

        // If not alive, then die.
        if (!IsAlive())
            Die(source);
    }

    public virtual void Heal(int hpr, Unit source)
    {
        // (source) is not needed for now, but it's good to have in future implementations
        // If this Char is not alive or the healing is not positive, return
        if (!IsAlive() || hpr < 0)
            return;

        // Increase HP by healing amount
        HP += hpr;

        // Debug:
        Debug.Log("[-] " + this + " has been healed, HP before gate: " + this.HP);

        // Gate HP to be max maxHP
        if (this.HP >= maxHP)
            HP = maxHP;

        // HP bar update
        if (healthbar)
            healthbar.setHealth(HP);
        else if (healthbarC)
            healthbarC.setHealthC(HP);
        else if (healthbarQ)
            healthbarQ.setHealthQ(HP);
    }

    protected void ClearAttacks()
    {
        availableAttacks.Clear();
    }

    protected void AddAttack(Vector2Int coords)
    {
        availableAttacks.Add(coords);
    }

    protected void AddAttacks(ICollection<Vector2Int> coordsCollection)
    {
        availableAttacks.UnionWith(coordsCollection);
    }

    protected void RemoveAttack(Vector2Int coords)
    {
        availableAttacks.Remove(coords);
    }

    protected void RemoveAttacks(ICollection<Vector2Int> coordsCollection)
    {
        availableAttacks.ExceptWith(coordsCollection);
    }
    #endregion

    // Utility (or misc, depends on my mood)
    #region Utility
    public void SetMaterial(Material material)
    {
        if (materialSetter == null)
            materialSetter = GetComponent<MaterialSetter>();
        if (materialSetter)
            materialSetter.SetSingleMaterial(material);
    }

    protected Unit GetUnitInDirection<T>(PlayerTeam team, Vector2Int direction) where T : Unit
    {
        for (int i = 1; i <= Field.FIELD_SIZE; i++)
        {
            Vector2Int nextCoords = OccupiedSquare + direction * i;
            Unit unit = Field.GetUnitOnSquare(nextCoords);

            if (!Field.CheckIfCoordsAreOnField(nextCoords))
                return null;
            if (unit != null)
            {
                if (unit.Team != team || !(unit is T))
                    return null;
                else if (unit.Team == team && unit is T)
                    return unit;
            }
        }
        return null;
    }

    // Temporary solution:
    // Update in-game UI, use with caution!
    public void UpdateUnitDetails()
    {
        Field.UpdateUnitDetails(this);
    }

    public override string ToString()
    {
        return "Unit " + this.id + " (" + this.GetType() + ")";
    }
    #endregion
}