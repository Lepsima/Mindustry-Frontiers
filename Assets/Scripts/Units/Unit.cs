using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Teams;
using Photon.Pun;
using System.Collections.Generic;
using System;
using UnityEngine;
using Frontiers.Content.Upgrades;

public abstract class Unit : Entity, IArmed {

    public new UnitType Type { protected set; get; }

    #region - References -
    protected Transform spriteHolder;
    protected SpriteRenderer teamSpriteRenderer;

    readonly List<Weapon> weapons = new();
    public bool unarmed = true;

    #endregion


    #region - Current Status Vars -
    protected Entity Target { 
        get { 
            return _target; 
        }  
        
        set {
            if (_target != value) { 
                _target = value;
                OnTargetChanged?.Invoke(this, new EntityArg { other = _target });
            }
        } 
    }
    private Entity _target;

    public TileType FloorTile {
        get { return _floorTile; }

        set {
            if (value != _floorTile) {
                _floorTile = value;
                OnFloorTileChange();
            }
        }
    }
    private TileType _floorTile;

    protected LandPadBlock currentLandPadBlock;
    protected event EventHandler<EntityArg> OnTargetChanged;
    protected Shadow shadow;

    protected UnitMode _mode = UnitMode.Return, lastUnitMode;
    protected AssistSubState subStateMode = AssistSubState.Waiting;

    protected float gForce, angularGForce;
    protected Vector2 acceleration;
    protected Vector2 velocity;

    protected Vector2 predictedTargetPosition;
    protected Vector2 homePosition;
    public Vector2 patrolPosition = Vector2.zero;

    protected float 
        targetPower, // The power percent that the engine should be at
        currentMass, // The current mass of this unit
        fuel, // The current fuel of this unit
        height, // The current height/altitude of this unit
        cargoMass, // The current mass of the cargo of this unit
        enginePower; // The current power at wich the engine works, based on targetPower and regulated by fuel and/or behaviour parameters

    protected bool
        isCoreUnit, // Wether this is a core/support unit
        isLanded, // Wether this unit is landed to a landpad/somewhere else or not
        isTakingOff, // Wether this unit is taking off from the previous land point
        areWeaponsActive, // Wether the weapons of the unit are active or not
        canLoseTarget = true; //Wether to update the target lost timer, used to not lose the current target when doing long maneuvers

    protected ConstructionBlock constructingBlock;

    // Timers
    protected float
        targetSearchTimer, // The next time the unit can search for a target
        landPadSearchTimer, // The next time the unit can search for a landing pad
        constructionSearchTimer, // The next time the unit can search for an unfinished building
        targetLostTimer, // The time without visual contact of a target at wich is considered a lost target 
        deactivateWeaponsTimer, // The time at wich the weapons will deactivate, used for more imprecise fighter/bomber building runs
        modeChangeRequestTimer; // A small cooldown to avoid multiple requests 

    // Parameters used by this unit's behaviour for the next physics update
    Vector2 _position;
    protected bool _rotate, _move;

    #endregion


    #region - Upgradable Stats -

    protected float 
        maxVelocity, rotationSpeed, itemPickupDistance, buildSpeedMultiplier, range,
        searchRange, fov, fuelCapacity, fuelConsumption, fuelRefillRate, emptyMass, fuelDensity;

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

    public virtual void ChangeMode(int mode, bool registerPrev) {
        if ((UnitMode)mode == UnitMode.Attack && unarmed) mode = (int)UnitMode.Patrol;
     
        lastUnitMode = registerPrev ? Mode : (UnitMode)mode;
        Mode = (UnitMode)mode;
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

    public enum AssistSubState {
        Waiting = 0,
        Collect = 1,
        Deposit = 2,
    }

    public UnitMode Mode { 
        set {
            if(value != _mode) {
                _mode = value;
                Target = null;

                targetLostTimer = 0;
                landPadSearchTimer = 0;
                targetSearchTimer = 0;
                deactivateWeaponsTimer = 0;

                homePosition = GetPosition();
                SetWeaponsActive(false);
            }
        } 

        get { 
            return _mode; 
        } 
    }

    protected virtual void Update() {
        teamSpriteRenderer.color = CellColor();
        if (shadow) shadow.SetDistance(height);
        FloorTile = GetGroundTile(); 

        if (deactivateWeaponsTimer <= Time.time) {
            if (deactivateWeaponsTimer != 0f) {
                SetWeaponsActive(false);
                deactivateWeaponsTimer = 0f;
            }
        }

        if (currentLandPadBlock != null && fuel < fuelCapacity) {
            if (fuel < fuelCapacity) { 
                fuel += currentLandPadBlock.Refuel(fuelRefillRate * Time.deltaTime);

                // When fuel is completely refilled, continue the previous mode
                if (fuel > fuelCapacity) {
                    fuel = fuelCapacity;
                    Client.UnitChangeMode(this, (int)lastUnitMode);
                }
            }
        }
    }

    //Physics management
    protected virtual void FixedUpdate() {
        enginePower = CalculateEnginePower();
        HandleBehaviour();
        HandlePhysics();
    }

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);
        UnitUpgradeMultipliers mult = upgrade.properties as UnitUpgradeMultipliers;

        maxVelocity += maxVelocity * mult.unit_maxVelocity;
        rotationSpeed += rotationSpeed * mult.unit_rotationSpeed;
        itemPickupDistance += itemPickupDistance * mult.unit_itemPickupDistance;
        buildSpeedMultiplier += buildSpeedMultiplier * mult.unit_buildSpeedMultiplier;
        range += range * mult.unit_range;
        searchRange += searchRange * mult.unit_searchRange;
        fov += fov * mult.unit_fov;
        fuelCapacity += fuelCapacity * mult.unit_fuelCapacity;
        fuelConsumption += fuelConsumption * mult.unit_fuelConsumption;
        fuelRefillRate += fuelRefillRate * mult.unit_fuelRefillRate;
        emptyMass += emptyMass * mult.unit_emptyMass;
        fuelDensity += fuelDensity * mult.unit_fuelDensity;
    }

    //Initialize the unit
    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        Type = type as UnitType;

        if (Type == null) {
            Debug.LogError("Specified type: " + type + ", is not valid for a unit");
            return;
        }

        name = Type.name + " " + teamCode;
        spriteHolder = transform.GetChild(0);
        CreateTransforms();

        base.Set(position, rotation, type, id, teamCode);

        transform.localScale = Vector3.one * Type.size;
        size = Type.size;
        hasItemInventory = true;

        maxVelocity = Type.maxVelocity;
        rotationSpeed = Type.rotationSpeed;
        itemPickupDistance = Type.itemPickupDistance;
        buildSpeedMultiplier = Type.buildSpeedMultiplier;
        range = Type.range;
        searchRange = Type.searchRange;
        fov = Type.fov;
        fuel = fuelCapacity = Type.fuelCapacity;
        fuelConsumption = Type.fuelConsumption;
        fuelRefillRate = Type.fuelRefillRate;
        emptyMass = Type.emptyMass;
        fuelDensity = Type.fuelDensity;

        height = Type.groundHeight / 2;

        SetWeapons();

        transform.SetPositionAndRotation(position, rotation);
        homePosition = transform.position;
        FloorTile = null;

        OnTargetChanged += OnTargetValueChange;

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

    public void SetVelocity(Vector2 velocity) => this.velocity = velocity;

    public override void SetInventory() {
        inventory = new Inventory(itemCapacity, Type.itemMass);
        hasItemInventory = true;

        base.SetInventory();
    }

    protected virtual void CreateTransforms() {
        shadow = transform.GetComponentInChildren<Shadow>();
        shadow.SetDistance(Type.groundHeight);
        shadow.SetSprite(Type.spriteFull);
    }

    public override EntityType GetEntityType() => Type;


    #endregion


    #region - Events -

    public override void OnInventoryValueChange(object sender, EventArgs e) {
        if (isCoreUnit) {
            if (Mode != UnitMode.Assist) return;
            UpdateSubBehaviour();
        }
    }

    protected void OnTargetInventoryValueChange(object sender, EventArgs e) {
        if (isCoreUnit) {
            if (Mode != UnitMode.Assist) return;
            UpdateSubBehaviour();
        }
    }

    protected virtual void OnTargetValueChange(object sender, EntityArg e) {
        if (isCoreUnit) {

        }
    }

    protected virtual void OnFloorTileChange() {

    }

    #endregion


    #region - Core Unit things - 
    public void UpdateSubBehaviour() {
        if (!constructingBlock && ConstructionBlock.blocksInConstruction.Count == 0) subStateMode = AssistSubState.Waiting;

        // If has enough or the max amount of items to build the block, go directly to it, else go to the core to refill
        bool hasMaxItems = inventory.HasToMax(constructingBlock.GetRestantItems());
        bool hasUsefulItems = inventory.Empty(constructingBlock.GetRequiredItems()) && !inventory.Empty();
        subStateMode = hasMaxItems || hasUsefulItems ? AssistSubState.Deposit : AssistSubState.Collect;
    }

    private ConstructionBlock TryGetConstructionBlock() {
        if (constructionSearchTimer > Time.time) return null;
        constructionSearchTimer = Time.time + 1f;

        ConstructionBlock closestBlock = null;
        float closestDistance = 100000f;

        foreach (ConstructionBlock block in ConstructionBlock.blocksInConstruction) {
            if (!Target.GetInventory().Has(block.GetRestantItems())) continue;
            float distance = Vector2.Distance(block.GetPosition(), GetPosition());

            if (distance < closestDistance) {
                closestDistance = distance;
                closestBlock = block;
            }
        }

        return closestBlock;
    }

    #endregion


    #region - Behaviour -
    public virtual void HandlePhysics() {
        // Calculate velocity and position
        velocity = Vector2.ClampMagnitude(acceleration * Time.fixedDeltaTime + velocity, maxVelocity);
        transform.position += (Vector3)velocity * Time.fixedDeltaTime;

        // Calculate g-force and reset acceleration
        gForce = (acceleration * Time.fixedDeltaTime).magnitude;
        acceleration = Vector2.zero;
    }

    public abstract void HandleHeight();

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

        Type.UpdateBehaviour(this, _position);
        HandleHeight();
    }

    protected void SetBehaviourPosition(Vector2 target) => _position = target;


    #region - Behaviours -
    protected virtual void AttackBehaviour() {
        HandleTargeting();

        if (!Target || unarmed) {
            if (lastUnitMode != UnitMode.Attack || unarmed) {
                Client.UnitChangeMode(this, (int)lastUnitMode);
                return;
            } else {
                Target = TeamUtilities.GetClosestCoreBlock(GetPosition(), TeamUtilities.GetEnemyTeam(teamCode));
                if (!Target) Client.UnitChangeMode(this, (int)UnitMode.Return);
            }
        }

        if (!Target) return;
        SetBehaviourPosition(Target.GetPosition());

        if (InRange(_position) && StopsToShoot()) _move = false;

        bool canShoot = InShootRange(_position, weapons[0].Type.maxTargetDeviation);
        if (canShoot != areWeaponsActive) SetWeaponsActive(canShoot);
    }

    protected virtual void PatrolBehaviour() {
        // Default target search
        HandleTargeting();

        // If target found, switch to attack
        if (Target) {     
            Client.UnitChangeMode(this, (int)UnitMode.Attack, true);
            return;
        } 
        
        if (Vector2.Distance(patrolPosition, GetPosition()) < 5f || patrolPosition == Vector2.zero) {
            // If no target or close to previous patrol point, create new one
            Client.UnitChangePatrolPoint(this, UnityEngine.Random.insideUnitCircle * searchRange + homePosition);
        } else {
            // Then move to the patrol point
            SetBehaviourPosition(patrolPosition);
        }
    }

    protected virtual void ReturnBehaviour() {
        //If target block is invalid, get closest landpad
        bool isInvalid = !Target || !(Target is LandPadBlock) || !(Target as LandPadBlock).CanLand(this);
        if (landPadSearchTimer < Time.time && isInvalid) {
            // Search for a landpad
            landPadSearchTimer = 1f + Time.time;
            LandPadBlock targetLandPad = MapManager.Map.GetBestAvilableLandPad(this);

            // Confirm target change
            if (targetLandPad) Target = targetLandPad;
        }

        // If couldnt't find any landpads, continue as patrol mode
        if (Target is LandPadBlock landPad) {
            //If close to landpad, land
            float distance = Vector2.Distance(Target.GetPosition(), GetPosition());

            // Check if it can land
            bool isInDistance = distance < landPad.Type.size / 2 + 0.5f;
            bool isMovingSlow = velocity.sqrMagnitude < 10f;
            bool isFlyingLow = height < 3f;
            bool canLand = landPad.CanLand(this);

            // Try to land
            if (isInDistance && isFlyingLow && isMovingSlow && canLand) Land();

            //Move towards target
            SetBehaviourPosition(Target.GetPosition());

        } else PatrolBehaviour();
    }

    protected virtual void AssistBehaviour() {   
        if (!isCoreUnit) {
            Client.UnitChangeMode(this, (int)UnitMode.Return);
            return;
        }

        // If there is no block to build, search for any other blocks in the map
        if (constructingBlock == null) {
            ConstructionBlock found = null;

            if (ConstructionBlock.blocksInConstruction.Count != 0) found = TryGetConstructionBlock();
            else subStateMode = AssistSubState.Waiting;

            if (found != null) {
                constructingBlock = found;
                UpdateSubBehaviour();
            }
        }

        // If there's nothing to do, go back to core and deposit all items
        if (subStateMode == AssistSubState.Waiting) {
            if (!Target) {
                Client.UnitChangeMode(this, (int)UnitMode.Idling, true);
                return;
            }

            if (inventory.Empty()) return;
            float distanceToCore = Vector2.Distance(Target.GetPosition(), GetPosition());

            if (distanceToCore < itemPickupDistance) {
                _move = false;

                // Drop items to core
                inventory.TransferAll(Target.GetInventory());
            } else {
                SetBehaviourPosition(Target.GetPosition());
            }
        }

        // If needs items to build block, go to core and pickup items
        if (subStateMode == AssistSubState.Collect) {
            if (!Target) {
                Client.UnitChangeMode(this, (int)UnitMode.Idling, true);
                return;
            }

            float distanceToCore = Vector2.Distance(Target.GetPosition(), GetPosition());

            if (distanceToCore < itemPickupDistance) {
                _move = false;

                // Drop items to core
                inventory.TransferAll(Target.GetInventory());

                // Get only useful items
                Target.GetInventory().TransferAll(inventory, ItemStack.ToItems(constructingBlock.GetRestantItems()));

            } else {
                SetBehaviourPosition(Target.GetPosition());
            }
        }

        // If has items to build block, go to the block and deposit them
        if (subStateMode == AssistSubState.Deposit) {
            float distanceToConstruction = Vector2.Distance(constructingBlock.GetPosition(), GetPosition());

            if (distanceToConstruction < itemPickupDistance) {
                _move = false;

                // Drops items on the constructing block
                inventory.TransferSubstractAmount(constructingBlock.GetInventory(), constructingBlock.GetRestantItems());
            } else {
                SetBehaviourPosition(constructingBlock.GetPosition());
            }
        }
    }

    protected virtual void IdlingBehaviour() {
        SetBehaviourPosition(homePosition);
    }

    #endregion

    public void TakeOff() {
        Client.UnitTakeOff(this);
    }

    public virtual void OnTakeOff() {

    }

    protected virtual void EndTakeOff() {
        //If is landed on a landpad, takeoff from it
        if (currentLandPadBlock) currentLandPadBlock.TakeOff(this);
        currentLandPadBlock = null;

        //Takeoff ended, allowing free movement
        isTakingOff = false;
    }

    public virtual void Land() {
        if (!Target) Crash();

        LandPadBlock landpad = Target as LandPadBlock;

        bool isLandpad = landpad != null;
        bool isNear = Vector2.Distance(Target.GetPosition(), transform.position) < Target.size * 0.65f;
        bool canDock = isLandpad && landpad.CanLand(this);

        if (isNear && canDock) Dock(landpad);
        else Crash();
    }

    public virtual void Dock(LandPadBlock landpad) {
        landpad.Land(this);
        currentLandPadBlock = landpad;

        //Set landed true and stop completely the unit
        isLanded = true;
        velocity = Vector2.zero;
    }

    public virtual void Crash() {
        Client.DestroyUnit(this, false);
    }

    /// <summary>
    /// Fully autonomous target search, includes:
    ///  - Target search cooldown, for performance and accurate network syncronization
    ///  - Target lost timer which the unit still remembers the target for a few seconds
    ///  - Optional default target enemy core
    /// </summary>
    /// <param name="useEnemyCoreAsDefault">Enables the default target</param>
    protected void HandleTargeting(bool useEnemyCoreAsDefault = false) {
        if (Target) {
            // Check if target is still valid
            bool inRange = (Target as Unit) == null ? InSearchRange(Target.GetPosition()) : InShootRange(Target.GetPosition(), fov);
            targetLostTimer = inRange || canLoseTarget ? 0 : targetLostTimer + Time.deltaTime;
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

    protected bool ValidTarget(Entity target) {
        if (!target) return false;
        return Vector2.Distance(target.GetPosition(), GetPosition()) < searchRange;
    }

    protected Entity GetTarget(Type[] priorityList = null) {
        //Default priority targets
        if (priorityList == null) priorityList = new Type[4] { typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(Block) };

        foreach(Type type in priorityList) {
            //Search the next priority type
            Entity tempTarget;

            if (type == typeof(Unit)) tempTarget = MapManager.Map.GetClosestEntityInView(GetPosition(), transform.up, fov, typeof(Unit), TeamUtilities.GetEnemyTeam(teamCode));
            else tempTarget = MapManager.Map.GetClosestEntity(GetPosition(), type, TeamUtilities.GetEnemyTeam(teamCode));

            //If target is valid, stop searching
            if (ValidTarget(tempTarget)) return tempTarget;
        }

        return null;
    }

    #endregion


    #region - Math & Getters - 
    public float GetHeight() => height;

    public virtual float GetTargetVelocity() {
        return maxVelocity;
    }

    public virtual TileType GetGroundTile() {
        // Get the map tile below the shadow (for 3d perspecive realism)
        Vector2 position = shadow.transform.position;
        if (!MapManager.Map.InBounds(position)) return null;
        return MapManager.Map.GetMapTileTypeAt(Map.MapLayer.Ground, position);
    }

    protected abstract bool StopsToShoot();

    float angle;
    public bool InShootRange(Vector2 target, float fov) {
        if (!InRange(target)) return false;

        Vector2 relative = target - GetPosition();
        angle = Vector2.Angle(relative, transform.up);
        return angle < fov;
    }

    public override Vector2 GetPredictedPosition(Vector2 origin, Vector2 velocity) {
        float time = (GetPosition() - origin).magnitude / (velocity - this.velocity).magnitude;
        Vector2 impact = GetPosition() + time * this.velocity;
        return impact;
    }

    public Vector2 GetBehaviourPosition() {
        return _position;
    }

    public float GetEnginePower() {
        return enginePower;
    }

    public virtual float CalculateEnginePower() {
        // Get the percent of power the engine should produce
        return fuel <= 0 || isLanded ? 0f : targetPower;
    }

    public virtual float GetRotationPower() {
        return 1;
    }

    public void ConsumeFuel(float amount) {
        // Consume fuel and update fuel mass
        fuel -= amount;
        UpdateCurrentMass();

        // If only 10s of fuel left, enable return mode
        if (fuel / fuelConsumption < Type.fuelLeftToReturn) Client.UnitChangeMode(this, (int)UnitMode.Return, true);
    }

    public virtual void UpdateCurrentMass() {
        float fuelMass = fuel * this.fuelDensity;
        currentMass = emptyMass + cargoMass + fuelMass;
    }

    public virtual bool CanMove() {
        return _move && !isLanded;
    }

    public virtual bool CanRotate() {
        return _rotate && !isLanded;
    }

    public void Accelerate(Vector2 amount) {
        acceleration += amount / currentMass;
    }

    public abstract void Tilt(float value);

    public abstract bool IsFleeing();

    public virtual bool IsWreck() => false;

    public bool InRange(Vector2 target) => Vector2.Distance(target, GetPosition()) < (range - 0.05f);

    public bool InSearchRange(Vector2 target) => Vector2.Distance(target, GetPosition()) < searchRange;

    public virtual Vector2 GetDirection(Vector2 target) => (target - GetPosition()).normalized;

    //How much is the second vector pointing like other vector (0 = exact, 1 == oposite)
    public float GetForwardDotProduct(Vector2 v1, Vector2 v2) => Vector2.Dot(v1.normalized, v2.normalized) / 2 + 0.5f;

    //How much is the second vector pointing like other vector (1 = exact, 0 == oposite)
    public float GetBackwardDotProduct(Vector2 v1, Vector2 v2) => (1 + Vector2.Dot(v1.normalized, v2.normalized)) / 2;

    public float GetSimilarity(Vector2 v1, Vector2 v2) => 1 - Vector2.Angle(v1, v2) / 180f;

    public Vector2 GetVelocity() => velocity;

    public Entity GetTarget() => Target;

    public Vector2 GetTargetPosition() => Target && !unarmed ? Target.GetPredictedPosition(transform.position, transform.up * weapons[0].Type.bulletType.velocity) : GetBehaviourPosition();

    public float FuelPercent() => fuel / fuelCapacity;

    public float AmmoPercent() => 1;

    public bool CanRequest() => modeChangeRequestTimer < Time.time;

    public void AddToRequestTimer() => modeChangeRequestTimer = Time.time + 0.1f;

    #endregion


    #region - Shooting -    

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        EffectPlayer.PlayEffect(Type.deathFX, transform.position, size);

        GameObject explosionBlastGameObject = Instantiate(AssetLoader.GetPrefab("explosion-blast"), GetPosition(), Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        Destroy(explosionBlastGameObject, 10f);

        MapManager.Map.RemoveUnit(this);
        Client.syncObjects.Remove(SyncID);

        base.OnDestroy();
    }

    public void MaintainWeaponsActive(float time) {
        deactivateWeaponsTimer = Time.time + time;
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
