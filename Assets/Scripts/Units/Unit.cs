using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Settings;
using Frontiers.Teams;
using Photon.Pun;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Unit : Entity, IArmed {
    public new UnitType Type { protected set; get; }
    public TileType underTile;

    #region - References -
    protected Transform spriteHolder;
    protected SpriteRenderer teamSpriteRenderer;

    TrailRenderer[] trailRenderers;
    [SerializeField] ParticleSystem waterDeviationEffect;

    readonly List<Weapon> weapons = new();
    public bool unarmed = true;

    #endregion


    #region - Current Status Vars -
    private Entity _target;
    protected Entity Target { get { return _target; }  set { if (_target != value) _target = value; } }
    private LandPadBlock currentLandPadBlock;
    private Shadow shadow;

    protected UnitMode _mode = UnitMode.Return, lastUnitMode;

    protected float gForce, angularGForce;
    protected Vector2 acceleration;
    protected Vector2 velocity;

    protected Vector2 predictedTargetPosition;
    protected Vector2 homePosition;
    public Vector2 patrolPosition = Vector2.zero;

    protected float targetSpeed, targetHeight, enginePower, currentMass, lightPercent = 0f;

    protected float fuel, height, cargoMass;
    bool isTakingOff, isLanded, isFleeing, areWeaponsActive;

    // Timers
    private float targetSearchTimer, landPadSearchTimer, targetLostTimer, deactivateWeaponsTimer;

    // The target position used in behaviours for the next frame
    Vector2 _position;

    // Conditions used ONLY on the next frame, then they are reseted
    protected bool _rotate, _move;

    #endregion


    #region - Syncronization -

    public override float[] GetSyncValues() {
        float[] values = base.GetSyncValues();
        values[2] = GetPosition().x;
        values[3] = GetPosition().y;
        values[4] = velocity.x;
        values[5] = velocity.y;
        values[6] = Target.SyncID;
        return values;
    }

    public override void ApplySyncValues(float[] values) {
        base.ApplySyncValues(values);
        
        transform.position = new(values[2], values[3]);
        velocity = new(values[4], values[5]);

        SyncronizableObject syncObject = Client.GetBySyncID((int)values[6]);
        _target = syncObject ? syncObject as Entity : null;
    }

    public void ChangeMode(int mode, bool registerPrev) {
        if ((UnitMode)mode == UnitMode.Attack && unarmed) mode = (int)UnitMode.Patrol;
        if (registerPrev) lastUnitMode = (UnitMode)mode;

        Mode = (UnitMode)mode;
        Target = null;
        isFleeing = false;

        targetLostTimer = 0;
        landPadSearchTimer = 0;
        targetSearchTimer = 0;
        deactivateWeaponsTimer = 0;

        homePosition = GetPosition();
        SetWeaponsActive(false);
    }

    [PunRPC]
    public void RPC_NewPatrolPoint(Vector2 point) {
        patrolPosition = point;
    }

    #endregion


    #region - Main -

    public enum UnitMode {
        Attack = 1,
        Patrol = 2,
        Return = 3,
        Assist = 4,
        Idling = 5
    }

    public UnitMode Mode { 
        set {
            _mode = value; 
            Target = null;
        } 

        get { 
            return _mode; 
        } 
    }

    protected virtual void Start() {
        // Add temporal ammo
        //inventory.Add(Items.missileX1, 10);
    }

    protected virtual void Update() {
        HandleBehaviour();

        if (deactivateWeaponsTimer <= Time.time) {

            if(deactivateWeaponsTimer != 0f) {
                SetWeaponsActive(false);
                deactivateWeaponsTimer = 0f;
            }
        }

        teamSpriteRenderer.color = CellColor();

        if (isLanded && fuel < Type.fuelCapacity) {
            if (fuel < Type.fuelCapacity) {
                fuel += Type.fuelRefillRate * Time.deltaTime;

                // When fuel is completely refilled, continue the previous mode
                if (fuel > Type.fuelCapacity) {
                    fuel = Type.fuelCapacity;
                    Client.UnitChangeMode(this, (int)lastUnitMode);
                }
            }
        }

        if (velocity.magnitude > 0) {
            underTile = GetGroundTile();
            ParticleSystem.EmissionModule emissionModule = waterDeviationEffect.emission;

            bool isWater = underTile != null && underTile.isWater;
            emissionModule.rateOverDistanceMultiplier = isWater ? 5f : 0f;
        }
    }

    //Physics management
    protected virtual void FixedUpdate() {
        HandlePhysics();
    }

    //Initialize the unit
    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        Type = type as UnitType;

        if (Type == null) {
            Debug.LogError("Specified type: " + type + ", is not valid for a unit");
            return;
        }

        name = "Unit Team : " + teamCode;
        spriteHolder = transform.GetChild(0);
        base.Set(position, rotation, type, id, teamCode);

        transform.localScale = Vector3.one * Type.size;
        size = Type.size;
        hasInventory = true;

        fuel = Type.fuelCapacity;
        height = Type.flyHeight / 2;
        health = Type.health;

        SetEffects();
        SetWeapons();

        transform.SetPositionAndRotation(position, rotation);
        homePosition = transform.position;

        syncValues = 7;

        MapManager.Map.AddUnit(this);
        Client.syncObjects.Add(SyncID, this);
    }

    protected override void SetSprites() {
        spriteHolder.GetComponent<SpriteRenderer>().sprite = Type.sprite;

        teamSpriteRenderer = SetOptionalSprite(spriteHolder.Find("Cell"), Type.cellSprite);
        SetOptionalSprite(spriteHolder.Find("Outline"), Type.outlineSprite);

        teamSpriteRenderer.color = teamCode == TeamUtilities.GetLocalTeam() ? TeamUtilities.LocalTeamColor : TeamUtilities.EnemyTeamColor;
    }

    //Set the Unit's weapons
    private void SetWeapons() {
        unarmed = Type.weapons.Length == 0;

        foreach (WeaponMount weaponMount in Type.weapons) {
            SetWeapon(weaponMount);

            //If mirrored, repeat previous steps
            if (weaponMount.mirrored) SetWeapon(weaponMount, true);     
        }
    }

    private void SetWeapon(WeaponMount weaponMount, bool mirrored = false) {
        //Get weapon prefab
        GameObject weaponPrefab = AssetLoader.GetPrefab("weaponPrefab");
        Vector3 offsetPos = mirrored ? new Vector3(-weaponMount.position.x, weaponMount.position.y, 0) : weaponMount.position;

        //Create and initialize new weapon
        GameObject weaponGameObject = Instantiate(weaponPrefab, transform.position, spriteHolder.transform.rotation);
        weaponGameObject.transform.parent = spriteHolder;
        weaponGameObject.transform.localPosition += offsetPos;
        weaponGameObject.transform.localScale = Vector3.one;

        Weapon weapon = weaponGameObject.AddComponent<Weapon>();
        weapon.Set(this, weapons.Count, weaponMount.weapon, mirrored, weaponMount.onTop);
        weapons.Add(weapon);
    }

    private void SetEffects() {
        trailRenderers = (TrailRenderer[])transform.GetComponentsInChildren<TrailRenderer>(true).Clone();
        shadow = transform.GetComponentInChildren<Shadow>();

        foreach (ParticleSystem particleSystem in gameObject.GetComponentsInChildren<ParticleSystem>()) {
            if (particleSystem.name == "WaterDeviationFX") waterDeviationEffect = particleSystem;
        }

        shadow.SetDistance(Type.flyHeight);
        shadow.SetSprite(Type.spriteFull);
    }

    private void SetDragTrailLenght(float time) {
        foreach (TrailRenderer tr in trailRenderers) tr.time = Mathf.Abs(time);
    } //Simulate drag trails


    public void SetVelocity(Vector2 velocity) => this.velocity = velocity;

    public override void SetInventory() {
        inventory = new Inventory(Type.itemCapacity, Type.itemMass);
        hasInventory = true;

        base.SetInventory();
    }

    public override void OnInventoryValueChange(object sender, System.EventArgs e) {
        // Update inventory things
    }

    public override EntityType GetEntityType() => Type;

    public float GetHeight() => height;

    public TileType GetGroundTile() {
        Vector2 position = shadow.transform.position;
        if (MapManager.Map.IsInBounds(position)) return null;
        return MapManager.Map.GetMapTileTypeAt(Map.MapLayer.Ground, position);
    }

    #endregion


    #region - Behaviour -
    /// <summary>
    /// The main function to handle the unit physics
    /// Handles: Movement, Rotation, Height and Fuel Consumption
    /// </summary>
    public void HandlePhysics() {
        // Get distance to behaviour target
        float distance = Vector2.Distance(_position, transform.position);

        // A value from 0 to 1 that indicates the power output percent of the engines
        enginePower = Mathf.Clamp01(fuel > 0f ? 1f : 0f) * targetSpeed;

        // If the current height is higher than should be, lower engine power on purpose
        if (height > targetHeight) enginePower *= 0.75f;
        enginePower *= height / Type.flyHeight;

        // Consume fuel based on fuelConsumption x enginePower
        fuel -= Type.fuelConsumption * enginePower * Time.fixedDeltaTime;

        float fuelMass = FuelPercent() * Type.fuelMass;
        float cargoMass = 0;
        currentMass = Type.emptyMass + fuelMass + cargoMass;

        // If only 10s of fuel left, enable return mode
        if (fuel / Type.fuelConsumption < 10f) Client.UnitChangeMode(this, (int)UnitMode.Return, true);

        if (_rotate) {
            // Power is reduced if: g-forces are high, is close to the target or if the behavoiur is fleeing
            float rotationPower = Mathf.Clamp01(2 / gForce);
            if (distance < 5f || isFleeing) rotationPower *= Mathf.Clamp01(distance / 10);

            // Quirky quaternion stuff to make the unit rotate slowly -DO NOT TOUCH-
            Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, (_position - GetPosition()).normalized);
            desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);

            float prevRotation = transform.eulerAngles.z;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, Type.rotationSpeed * rotationPower * Time.fixedDeltaTime);
            angularGForce = (transform.eulerAngles.z - prevRotation) * Time.deltaTime * 10f;
        }

        if (_move) {
            // If isn't landed or taking off, calculate normal movement physics
            if (!isLanded && !isTakingOff) {

                // Get the direction
                Vector2 direction = GetDirection(_position);
                Vector2 targetDirection = (_position - GetPosition()).normalized;

                // Get acceleration and drag values based on direction
                float similarity = GetSimilarity(transform.up, targetDirection);
                enginePower *= isFleeing ? 1 : (similarity > 0.5f ? similarity : 0.1f);

                acceleration += enginePower * Type.force / currentMass * direction.normalized;

                // Tilt the unit according to accelDot
                spriteHolder.localEulerAngles = new Vector3(0, Mathf.LerpAngle(spriteHolder.localEulerAngles.y, angularGForce * Type.bankAmount, Type.bankSpeed * Time.fixedDeltaTime), 0);
            }
        }

        // Height behaviour
        if (!isLanded) {
            // If is taking off climb until half fly height
            if (isTakingOff) height = Mathf.Clamp(Time.fixedDeltaTime * Type.flyHeight / 6 + height, 0, Type.flyHeight);
            else {
                float liftForce = 3;
                float fallForce = -3;

                // Change height increase or decrease based on enginePower
                bool isFalling = enginePower <= 0 || targetHeight < height;
                height = Mathf.Clamp((isFalling ? fallForce : liftForce) * Time.fixedDeltaTime + height, 0, Type.flyHeight);

                // If is touching ground, crash
                if (height < 0.05f) Land();
            }
        }

        // Drag force inversely proportional to velocity
        acceleration -= (1 - Type.drag * 0.33f * Time.fixedDeltaTime) * (velocity * transform.up);

        // Drag force inversely proportional to direction
        acceleration -= (1 - Type.drag * 0.67f * Time.fixedDeltaTime) * velocity;

        // Calculate velocity and position
        velocity = Vector2.ClampMagnitude(acceleration * Time.fixedDeltaTime + velocity, Type.velocityCap);
        transform.position += (Vector3)velocity * Time.fixedDeltaTime;

        // Calculate g-force and reset acceleration
        gForce = (acceleration * Time.fixedDeltaTime).magnitude;
        acceleration = Vector2.zero;
    }

    public void HandleBehaviour() {
        _move = true;
        _rotate = true;

        switch (Mode) {
            case UnitMode.Attack:
                AttackBehaviour();
                break;

            case UnitMode.Patrol:
                PatrolBehaviour();
                break;

            case UnitMode.Return:
                ReturnBehaviour();
                break;

            case UnitMode.Assist:
                AssistBehaviour();
                break;

            case UnitMode.Idling:
                IdlingBehaviour();
                break;
        }

        if (!Target) SetWeaponsActive(false);

        // Visual things
        SetDragTrailLenght(gForce * 0.3f);
        shadow.SetDistance(height);
        //frontLight.intensity = lightPercent;
    }

    protected void SetBehaviourPosition(Vector2 target) => _position = target;


    #region - Behaviours -
    protected virtual void AttackBehaviour() {
        targetHeight = Type.flyHeight;
        targetSpeed = 1f;

        //If is landed, takeoff
        if (isLanded) Client.UnitTakeOff(this);
        else if (!isTakingOff) {
            // Default target search
            if (!isFleeing) HandleTargeting(true);
            else {
                if (Vector2.Distance(patrolPosition, GetPosition()) < 5f) isFleeing = false;
                else SetBehaviourPosition(patrolPosition);
            }

            // If there's not even an enemy core, set unit to return
            if (!Target) {
                Client.UnitChangeMode(this, (int)UnitMode.Return);
            } else if (!isFleeing) {
                AttackSubBehaviour(Target.GetPosition());
            }

            // Once gets too close, run far away for the next run
            if (Type.useAerodynamics && Vector2.Distance(_position, transform.position) < 2f) { 
                isFleeing = true;

                // Where 25f is the flee distance
                patrolPosition = GetPosition() + (Vector2)transform.up * 25f;

                // Turn off weapons
                deactivateWeaponsTimer = Time.time + 0.75f;
            }
        }
    }

    protected virtual void PatrolBehaviour() {
        targetHeight = Type.flyHeight;
        targetSpeed = 0.9f;

        //If is landed, takeoff
        if (isLanded) TakeOff();
        else if (!isTakingOff) {
            // Default target search
            HandleTargeting();


            if (Target) {

                // If a target was found, attack
                AttackSubBehaviour(Target.GetPosition());

            } else if (Vector2.Distance(patrolPosition, GetPosition()) < 5f || patrolPosition == Vector2.zero) {

                // If no target or close to previous patrol point, create new one
                Client.UnitChangePatrolPoint(this, UnityEngine.Random.insideUnitCircle * Type.searchRange + homePosition);

            } else {

                // Then move to the patrol point
                SetBehaviourPosition(patrolPosition); 

            }
        }
    }

    protected virtual void ReturnBehaviour() {
        if (isLanded) return;

        targetHeight = Type.flyHeight * 0.5f;
        targetSpeed = 0.5f;

        //If target block is invalid, get closest landpad
        bool isInvalid = !Target || !(Target is LandPadBlock) || !(Target as LandPadBlock).CanLand(this);
        if (landPadSearchTimer < Time.time && isInvalid) {
            // Search for a landpad
            landPadSearchTimer = 3f + Time.time;
            LandPadBlock targetLandPad = MapManager.Map.GetBestAvilableLandPad(this);

            // Confirm target change
            if (targetLandPad) Target = targetLandPad;
        }
        
        // If couldnt't find any landpads, continue as patrol mode
        if (Target is LandPadBlock) {
            //If close to landpad, land
            float distance = Vector2.Distance(Target.GetPosition(), GetPosition());
            if (distance < ((Block)Target).Type.size / 2 + 0.5f && velocity.magnitude < 5f) Land(Target as LandPadBlock);

            //Move towards target
            SetBehaviourPosition(Target.GetPosition());

        } else PatrolBehaviour();
        
    }

    protected virtual void AssistBehaviour() {
        targetHeight = Type.flyHeight;
        targetSpeed = 1f;
    }

    protected virtual void IdlingBehaviour() {
        targetHeight = Type.flyHeight * 0.5f;
        targetSpeed = 0.5f;
    }

    #endregion

    #region - Sub Behaviours -

    /// <summary>
    /// A sub behaviour used for attacking a target at any time in the main behaviour
    /// </summary>
    /// <param name="target">The target's position</param>
    private void AttackSubBehaviour(Vector2 target) {
        if (unarmed) return;
        if (!Type.useAerodynamics && InRange(target)) _move = false;

        SetBehaviourPosition(target);

        if (Target) {
            bool canShoot = InShootRange(target, weapons[0].Type.maxTargetDeviation);
            if (canShoot != areWeaponsActive) SetWeaponsActive(canShoot);
        } else {
            SetWeaponsActive(false);
        }
    }

    #endregion

    /// <summary>
    /// Fully autonomous target search, includes:
    ///  - Target search cooldown, for performance and accurate network syncronization
    ///  - Target lost timer which the unit still remembers the target for a few seconds
    ///  - Optional default target enemy core
    /// </summary>
    /// <param name="useEnemyCoreAsDefault">Enables the default target</param>
    private void HandleTargeting(bool useEnemyCoreAsDefault = false) {
        if (Target) {
            // Check if target is still valid
            bool inRange = (Target as Unit) == null ? InSearchRange(Target.GetPosition()) : InShootRange(Target.GetPosition(), Type.fov);
            targetLostTimer = inRange ? 0 : targetLostTimer + Time.deltaTime;
        }

        if ((Target == null && targetSearchTimer < Time.time) || targetLostTimer > 2f) {
            // Update target timers
            targetSearchTimer = Time.time + 1.5f;
            targetLostTimer = 0f;

            // Find target or get closest
            Entity tempTarget = GetTarget(Type.priorityList);
            if (!tempTarget && useEnemyCoreAsDefault) tempTarget = TeamUtilities.GetClosestCoreBlock(GetPosition(), TeamUtilities.GetEnemyTeam(teamCode));     
            Target = tempTarget;
        }
    }

    private bool ValidTarget(Entity target) {
        if (!target) return false;
        return Vector2.Distance(target.GetPosition(), GetPosition()) < Type.searchRange;
    }

    private Entity GetTarget(Type[] priorityList = null) {
        //Default priority targets
        if (priorityList == null) priorityList = new Type[4] { typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(Block) };

        foreach(Type type in priorityList) {
            //Search the next priority type
            Entity tempTarget;

            if (type == typeof(Unit)) tempTarget = MapManager.Map.GetClosestEntityInView(GetPosition(), transform.up, Type.fov, typeof(Unit), TeamUtilities.GetEnemyTeam(teamCode));
            else tempTarget = MapManager.Map.GetClosestEntity(GetPosition(), type, TeamUtilities.GetEnemyTeam(teamCode));

            //If target is valid, stop searching
            if (ValidTarget(tempTarget)) return tempTarget;
        }

        return null;
    }

    #endregion


    #region - Landing / Takeoff - 

    //Land unit on the ground, if obstructed: crash, THIS CURRENTLY CRASHES(into the map) THE UNIT ALWAYS
    public void Land() {
        MapCrash();
        /*
        //If there's no ground to land, crash
        if (!MapManager.Instance.GetMapTileAt(transform.position)) {
            MapCrash();
            return;
        }

        //If there's a solid block on the landsite, crash
        if (MapManager.Instance.GetSolidTileAt(transform.position) && MapManager.Instance.GetBlockAt(Vector2Int.CeilToInt(transform.position)).Type.solid) MapCrash();

        //Set landed true and stop completely the unit
        isLanded = true;
        velocity = Vector2.zero;
        spriteHolder.localScale = Vector3.one * 0.7f;
        SetTrailTime(0);
        */
    }
   
    public void Land(LandPadBlock landPad) {
        //Land on landpad
        if (!landPad.Land(this)) return;
        currentLandPadBlock = landPad;

        //Set landed true and stop completely the unit
        isLanded = true;
        velocity = Vector2.zero;
        //spriteHolder.localScale = Vector3.one * 0.7f;

        height = 0f;
        SetDragTrailLenght(0);
    } //Land unit on a near landpad


    //When crash into the map
    public void MapCrash() {
        //Crash effects: TODO
        Client.DestroyUnit(this, true);
    }


    //Take off from land
    public void TakeOff() {
        if (isTakingOff || !isLanded) return;

        //Start takeOff
        isTakingOff = true;
        isLanded = false;
        velocity = Vector2.zero;

        //Allow free movement in ±3s
        Invoke(nameof(EndTakeOff), 3f);

        //Play particle system
        Effect.PlayEffect("TakeoffFX", transform.position, size);
    }


    //Enable physics movement
    private void EndTakeOff() {
        //If is landed on a landpad, takeoff from it
        if (currentLandPadBlock) currentLandPadBlock.TakeOff(this);
        currentLandPadBlock = null;

        //Takeoff ended, allowing free movement
        isTakingOff = false;
        //spriteHolder.localScale = Vector3.one;
        velocity = Type.force / 3 * transform.up;
    }
    #endregion


    #region - Math - 
    public float angle;
    public bool InShootRange(Vector2 target, float fov) {
        if (!InRange(target)) return false;

        float cosAngle = Vector2.Dot((target - GetPosition()).normalized, transform.up);
        angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;

        return angle < fov;
    }

    public override Vector2 GetPredictedPosition(Vector2 origin, Vector2 velocity) {
        float time = (GetPosition() - origin).magnitude / (velocity - this.velocity).magnitude;
        Vector2 impact = GetPosition() + time * this.velocity;
        return impact;
    }

    public bool InRange(Vector2 target) => Vector2.Distance(target, GetPosition()) < Type.range;

    public bool InSearchRange(Vector2 target) => Vector2.Distance(target, GetPosition()) < Type.searchRange;

    public Vector2 GetDirection(Vector2 target) => Type.useAerodynamics ? (Vector2)transform.up : (target - GetPosition()).normalized;

    //How much is the second vector pointing like other vector (0 = exact, 1 == oposite)
    public float GetForwardDotProduct(Vector2 v1, Vector2 v2) => Vector2.Dot(v1.normalized, v2.normalized) / 2 + 0.5f;

    //How much is the second vector pointing like other vector (1 = exact, 0 == oposite)
    public float GetBackwardDotProduct(Vector2 v1, Vector2 v2) => (1 + Vector2.Dot(v1.normalized, v2.normalized)) / 2;

    public float GetSimilarity(Vector2 v1, Vector2 v2) => 1 - Vector2.Angle(v1, v2) / 180f;

    public Vector2 GetVelocity() => velocity;

    public float HealthPercent() => health / Type.health;

    public float FuelPercent() => fuel / Type.fuelCapacity;

    public float AmmoPercent() => 1;

    #endregion


    #region - Shooting -    

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        Effect.PlayEffect(Type.explosionFX, transform.position, size);

        GameObject explosionBlastGameObject = Instantiate(AssetLoader.GetPrefab("explosion-blast"), GetPosition(), Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        Destroy(explosionBlastGameObject, 10f);

        MapManager.Map.RemoveUnit(this);
        Client.syncObjects.Remove(SyncID);

        base.OnDestroy();
    }

    public void SetWeaponsActive(bool value) {
        foreach (Weapon weapon in weapons) weapon.SetActive(value);
        areWeaponsActive = value;
    }

    public Weapon GetWeaponByID(int ID) {
        return weapons[ID];
    }
    #endregion
}
