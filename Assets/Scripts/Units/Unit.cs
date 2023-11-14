using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Teams;
using Photon.Pun;
using System.Collections.Generic;
using System;
using UnityEngine;
using Frontiers.Content.Upgrades;
using Frontiers.Squadrons;
using Action = Frontiers.Squadrons.Action;

public abstract class Unit : Entity, IArmed, IMessager {

    public new UnitType Type { protected set; get; }

    #region - References -
    protected Transform spriteHolder;
    protected SpriteRenderer teamSpriteRenderer;

    readonly List<Weapon> weapons = new();
    public bool unarmed = true;

    public string squadronName;
    public Squadron squadron;

    #endregion


    #region - Current Status Vars -

    public Action action;

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

    protected UnitMode _mode;
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
        ammo = float.MaxValue, // The current ammo amount of this unit
        height, // The current height/altitude of this unit
        cargoMass, // The current mass of the cargo of this unit
        enginePower; // The current power at wich the engine works, based on targetPower and regulated by fuel and/or behaviour parameters

    protected bool
        isCoreUnit, // Wether this is a core/support unit
        isLanded, // Wether this unit is landed to a landpad/somewhere else or not
        isTakingOff, // Wether this unit is taking off from the previous land point
        areWeaponsActive, // Wether the weapons of the unit are active or not
        canLoseTarget = true, //Wether to update the target lost timer, used to not lose the current target when doing long maneuvers
        inTargetMessageShown = false; // Whether if this unit has shown the "in target" message, resets for every action

    // Timers
    protected float
        damagedMessageTimer, // The next time the unit can show the "damaged" message
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

    public override int[] GetSyncData() {
        int[] data = base.GetSyncData();

        // Current target
        Entity target = GetTarget();
        data[1] = target ? target.SyncID : -1;

        // Position
        Vector2 position = GetPosition();
        data[2] = (int)(position.x * 1000f);
        data[3] = (int)(position.y * 1000f);

        // Velocity
        data[4] = (int)(velocity.x * 1000f);
        data[5] = (int)(velocity.y * 1000f);

        // Fuel
        data[6] = (int)(fuel * 1000f);

        return data;
    }

    public override void ApplySyncData(int[] values) {
        base.ApplySyncData(values);

        SyncronizableObject syncObject = Client.GetBySyncID((short)values[2]);
        Target = syncObject ? syncObject as Entity : null;

        transform.position = new(values[2] / 1000f, values[3] / 1000f);
        velocity = new(values[4] / 1000f, values[5] / 1000f);

        fuel = values[6] / 1000f;
    }

    public override string SaveDataToString(bool includeSyncID) {
        string data = base.SaveDataToString(includeSyncID);

        // Current target
        Entity target = GetTarget();
        data += (target ? target.SyncID : -1) + ":";

        // Position
        Vector2 position = GetPosition();
        data += (int)(position.x * 1000f) + ":" + (int)(position.y * 1000f) + ":";

        // Rotation
        data += (short)(transform.eulerAngles.z * 1000f);

        // Velocity
        data += (int)(velocity.x * 1000f) + ":" + (int)(velocity.y * 1000f) + ":";

        // Fuel
        data += (int)(fuel * 1000f) + ":";

        return data;
    }

    public override void ApplySaveData(string[] values, int i = 3) {
        base.ApplySaveData(values, i);

        SyncronizableObject syncObject = Client.GetBySyncID(short.Parse(values[i + 1]));
        Target = syncObject ? syncObject as Entity : null;

        velocity = new(int.Parse(values[i + 5]) / 1000f, int.Parse(values[i + 6]) / 1000f);

        fuel = int.Parse(values[i + 7]) / 1000f;
    }

    public void SetAction(Action action) {
        this.action = action;
        Mode = action.ToMode();

        if (Mode == UnitMode.Idling) Message(UnitMessages.Waiting);
        else Message(UnitMessages.Moving);

        inTargetMessageShown = false;
    }

    public void EnterTemporalMode(UnitMode mode) {
        Mode = mode;
    }

    public void ExitTemporalMode() {
        Mode = action.ToMode();
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
            if (value != _mode) {
                _mode = value;
                Target = null;

                targetLostTimer = 0;
                landPadSearchTimer = 0;
                targetSearchTimer = 0;
                deactivateWeaponsTimer = 0;

                patrolPosition = Vector2.zero;
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

        if (statusEventTimer <= Time.time) ShowMessage();

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
                    ExitTemporalMode();
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

        size = Type.size;
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        collider.size = Vector2.one * size;

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
        homePosition = TeamUtilities.GetClosestCoreBlock(transform.position, teamCode).GetPosition();
        FloorTile = null;

        OnTargetChanged += OnTargetValueChange;

        syncs = true; // Units always sync, if not desired just make "syncTime" really high
        syncValues = 7; // The amount of values sent each sync (do not change)
        syncTime = 3f; // The minimum time between syncs

        MapManager.Map.AddUnit(this);
        Client.syncObjects.Add(SyncID, this);
        
        SetAction(new(3, 0, Vector2.zero));
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
            if (weaponMount.mirrored)
                SetWeapon(weaponMount, true);
        }
    }

    private void SetWeapon(WeaponMount weaponMount, bool mirrored = false) {
        //Get weapon prefab
        GameObject weaponPrefab = AssetLoader.GetPrefab("WeaponPrefab");
        Vector3 offsetPos = mirrored ? new Vector3(-weaponMount.position.x, weaponMount.position.y, 0) : weaponMount.position;

        //Create and initialize new weapon
        GameObject weaponGameObject = Instantiate(weaponPrefab, transform.position, spriteHolder.transform.rotation);
        weaponGameObject.transform.parent = spriteHolder;
        weaponGameObject.transform.localPosition += offsetPos;
        weaponGameObject.transform.localScale = Vector3.one;

        Weapon weapon = weaponGameObject.AddComponent<Weapon>();
        weapon.Set(this, weaponMount.weapon, mirrored, weaponMount.onTop);
        weapons.Add(weapon);
    }

    public void SetVelocity(Vector2 velocity) => this.velocity = velocity;

    protected virtual void CreateTransforms() {
        shadow = transform.GetComponentInChildren<Shadow>();
        shadow.SetDistance(Type.groundHeight);
        shadow.SetSprite(Type.spriteFull);
    }

    public override EntityType GetEntityType() => Type;


    #endregion


    #region - Events -

    protected virtual void OnTargetValueChange(object sender, EntityArg e) {
        if (isCoreUnit) {
            // I cant remember what was this for...
        }
    }

    protected virtual void OnFloorTileChange() {

    }

    #endregion


    #region - Behaviour -
    public virtual void HandlePhysics() {
        // Calculate velocity and position
        velocity = Vector2.ClampMagnitude(acceleration * Time.fixedDeltaTime + velocity, maxVelocity);
        MoveTo(transform.position + (Vector3)velocity * Time.fixedDeltaTime);

        // Calculate g-force and reset acceleration
        gForce = (acceleration * Time.fixedDeltaTime).magnitude;
        acceleration = Vector2.zero;
    }

    public virtual void MoveTo(Vector2 position) {
        transform.position = position;
    }

    public abstract void HandleHeight();

    public virtual void UpdateBehaviour(Vector2 position) {

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

        UpdateBehaviour(_position);
        HandleHeight();
    }

    protected void SetBehaviourPosition(Vector2 target) => _position = target;


    #region - Behaviours -
    protected virtual void AttackBehaviour() {
        if (unarmed) EnterTemporalMode(UnitMode.Return);
        HandleTargeting();

        if (!Target) {
            // If has lost it's target and this is not the main mode, return to original mode
            if (action.ToMode() != UnitMode.Attack) ExitTemporalMode();

            // Else go to action position
            else SetBehaviourPosition(action.position);
            return;
        }

        SetBehaviourPosition(Target.GetPosition());

        if (InRange(_position) && StopsToShoot()) _move = false;

        if (!inTargetMessageShown && InRange(action.position)) { 
            Message(UnitMessages.TargetLost);
            inTargetMessageShown = true;
        }

        bool canShoot = InShootRange(_position, weapons[0].Type.maxTargetDeviation);
        if (canShoot != areWeaponsActive) SetWeaponsActive(canShoot);
    }

    protected virtual void PatrolBehaviour() {
        HandleTargeting();

        // If target found, switch to attack
        if (Target) {
            EnterTemporalMode(UnitMode.Attack);
            return;
        }

        if (Vector2.Distance(patrolPosition, GetPosition()) < 5f || patrolPosition == Vector2.zero) {
            Vector2 patrolPos = action == null ? homePosition : action.position;

            // If no target or close to previous patrol point, create new one
            Client.UnitChangePatrolPoint(this, UnityEngine.Random.insideUnitCircle * searchRange + patrolPos);
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

        } else
            PatrolBehaviour();
    }

    protected virtual void AssistBehaviour() {
        if (!isCoreUnit) {
            EnterTemporalMode(UnitMode.Return);
            return;
        }
    }

    protected virtual void IdlingBehaviour() {
        SetBehaviourPosition(homePosition);
    }

    #endregion

    public void TakeOff() {
        Client.UnitTakeOff(this);
        Message(UnitMessages.TakingOff);
    }

    public virtual void OnTakeOff() {

    }

    protected virtual void EndTakeOff() {
        //If is landed on a landpad, takeoff from it
        if (currentLandPadBlock)
            currentLandPadBlock.TakeOff(this);
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
        Message(UnitMessages.Landing);

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
    protected void HandleTargeting() {
        if (Target) {
            // Check if target is still valid
            bool inRange = (Target as Unit) == null ? InSearchRange(Target.GetPosition()) : InShootRange(Target.GetPosition(), fov);
            targetLostTimer = inRange || canLoseTarget ? 0 : targetLostTimer + Time.deltaTime;

            // If the target is lost, message
            if (targetLostTimer > 2f) Message(UnitMessages.TargetLost);
        }

        if ((Target == null && targetSearchTimer < Time.time) || targetLostTimer > 2f) {
            // Update target timers
            targetSearchTimer = Time.time + 1.5f;
            targetLostTimer = 0f;

            // Find target or get closest 
            Target = GetTarget(Type.priorityList);
            if (Target != null) Message(UnitMessages.TargetAdquired);
        }
    }

    protected bool ValidTarget(Entity target) {
        if (!target)
            return false;
        return Vector2.Distance(target.GetPosition(), GetPosition()) < searchRange;
    }

    protected Entity GetTarget(Type[] priorityList = null) {
        //Default priority targets
        if (priorityList == null)
            priorityList = new Type[4] { typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(Block) };

        foreach (Type type in priorityList) {
            //Search the next priority type
            Entity tempTarget;

            if (type == typeof(Unit))
                tempTarget = MapManager.Map.GetClosestEntityInView(GetPosition(), transform.up, fov, typeof(Unit), TeamUtilities.GetEnemyTeam(teamCode));
            else
                tempTarget = MapManager.Map.GetClosestEntity(GetPosition(), type, TeamUtilities.GetEnemyTeam(teamCode));

            //If target is valid, stop searching
            if (ValidTarget(tempTarget))
                return tempTarget;
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
        if (!MapManager.Map.InBounds(position))
            return null;
        return MapManager.Map.GetMapTileTypeAt(Map.MapLayer.Ground, position);
    }

    protected abstract bool StopsToShoot();

    float angle;
    public bool InShootRange(Vector2 target, float fov) {
        if (!InRange(target))
            return false;

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
        if (fuel / fuelConsumption < Type.fuelLeftToReturn) { 
            EnterTemporalMode(UnitMode.Return);
            Message(UnitMessages.Refuel);
        }
    }

    public virtual void UpdateCurrentMass() {
        float fuelMass = fuel * fuelDensity;
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

    public Vector2 GetAimPosition() => Target && !unarmed ? Target.GetPredictedPosition(transform.position, transform.up * weapons[0].Type.bulletType.velocity) : GetBehaviourPosition();

    public float FuelPercent() => fuel / fuelCapacity;

    public float AmmoPercent() => 1;

    public bool CanRequest() => modeChangeRequestTimer < Time.time;

    public void AddToRequestTimer() => modeChangeRequestTimer = Time.time + 0.1f;

    #endregion


    #region - Messages - 

    protected float statusEventTimer;
    protected MessageHandler.StatusEvent statusEvent;

    public void Message(MessageHandler.StatusEvent statusEvent) {
        if (this.statusEvent != null && this.statusEvent.priority >= statusEvent.priority) return;
        else this.statusEvent = statusEvent;
    }

    public void ShowMessage() {
        // If there is no message, wait a bit until next
        if (statusEvent == null) { 
            statusEventTimer = Time.time + 2f;
            return;
        }

        // Send message to message handler
        statusEventTimer = Time.time + statusEvent.displayTime;
        MessageHandler.Instance.HandleEvent(statusEvent, this);

        // This message has been shown, wait for next one
        statusEvent = null;
    }

    public override string GetName() {
        return squadronName;
    }

    #endregion


    #region - Shooting -    

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded)
            return;

        Message(UnitMessages.Destroyed);
        EffectPlayer.PlayEffect(Type.deathFX, transform.position, size);

        GameObject explosionBlastGameObject = Instantiate(AssetLoader.GetPrefab("ExplosionBlastPrefab"), GetPosition(), Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
        Destroy(explosionBlastGameObject, 10f);

        MapManager.Map.RemoveUnit(this);
        Client.syncObjects.Remove(SyncID);

        base.OnDestroy();
    }

    protected override void OnHealthChange() {
        base.OnHealthChange();

        if (damagedMessageTimer <= Time.time) {
            damagedMessageTimer = Time.time + 5f;
            Message(UnitMessages.Damaged);
        }
    }

    public void MaintainWeaponsActive(float time) {
        deactivateWeaponsTimer = Time.time + time;
    }

    public void SetWeaponsActive(bool value) {
        foreach (Weapon weapon in weapons)
            weapon.SetActive(value);
        areWeaponsActive = value;
    }

    public void ConsumeAmmo(float amount) {
        ammo -= amount;
    }

    public bool CanConsumeAmmo(float amount) {
        return ammo >= amount;
    }

    #endregion
}