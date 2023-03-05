using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;
using Frontiers.Squadrons;
using Frontiers.Settings;

public class Unit : LoadableContent, IDamageable, IView, IInventory {

    #region - References -
    public PhotonView PhotonView { get; set; }
    protected Transform spriteHolder;

    TrailRenderer[] trailRenderers;
    ParticleSystem takeOffEffect;

    readonly List<Weapon> weapons = new List<Weapon>();

    #endregion


    #region - Stats -
    public UnitType Type { protected set; get; }

    protected float velocityCap, accel, lowAccel, drag, bankAmount, bankSpeed, rotationSpeed;
    protected bool canVTOL;

    #endregion


    #region - Current Status Vars -
    private LoadableContent _target;
    protected LoadableContent Target { get { return _target; }  set { if (_target != value) _target = value; } }
    private LandPadBlock currentLandPadBlock;

    protected UnitMode _mode, lastUnitMode;

    protected ItemList inventory;
    protected UnitUI unitUI;

    protected Vector2 velocity;
    protected Vector2 patrolPosition = Vector2.zero;
    protected Vector2 homePosition;

    protected float health, fuel, nextSyncTime, nextTargetSearchTime, landPadSearchTime, timeSinceTargetLost;
    bool isTakingOff, isLanded;

    #endregion


    #region - Inventory - 

    public void SetInventory() {
        inventory = new ItemList(Type.itemCapacity, false);
        OnInventoryValueChange();
    }

    public ItemStack AddItems(ItemStack value) {
        ItemStack itemStack = inventory.AddItem(value);
        OnInventoryValueChange();
        return itemStack;
    }

    // Not abilable for units
    public ItemList AddItems(ItemStack[] value) => null;
    public ItemList SubstractItems(ItemStack[] value) => null;

    public ItemStack SubstractItems(ItemStack value) {
        ItemStack itemStack = inventory.SubstractItem(value);
        OnInventoryValueChange();
        return itemStack;
    }

    public ItemList GetItemList() {
        return inventory;
    }

    protected void OnInventoryValueChange() {
        unitUI.UpdateItem(inventory.GetStack());
    }

    protected void UpdateUISliders() {
        unitUI.UpdateSliders(HealthPercent(), FuelPercent(), AmmoPercent());
    }

    #endregion


    #region - Syncronization -

    [PunRPC]
    public void RPC_Takeoff() => TakeOff(false);

    [PunRPC]
    public void RPC_UnitMode(int mode, bool registerPrev) {
        if (registerPrev) lastUnitMode = (UnitMode)mode; 

        Mode = (UnitMode)mode;
        Target = null;
        homePosition = GetPosition();
        SetWeaponFullAuto(false);
    }

    public void SetMode(UnitMode mode, bool registerPrev = true) {
        if (mode != Mode) PhotonView.RPC(nameof(RPC_UnitMode), RpcTarget.All, (int)mode, registerPrev);
    }

    public void Sync() {
        nextSyncTime = Time.time + GetTimeCode() + State.SYNC_TIME;

        Vector2 p = transform.position;
        Vector2 v = velocity;

        PhotonView.RPC(nameof(RPC_SyncStatus), RpcTarget.Others, p.x, p.y, v.x, v.y, health, fuel);
    }

    [PunRPC]
    public void RPC_SyncStatus(float px, float py, float vx, float vy, float currentHealth, float currentFuel) {
        transform.position = new Vector3(px, py, 0);
        velocity = new Vector2(vx, vy);
        health = currentHealth;
        fuel = currentFuel;
    }

    public void SetNewPatrolPoint() {
        PhotonView.RPC(nameof(RPC_NewPatrolPoint), RpcTarget.All, Random.insideUnitCircle * Type.range + homePosition);
    }

    [PunRPC]
    public void RPC_NewPatrolPoint(Vector2 point) {
        patrolPosition = point;
    }

    #endregion

    public enum UnitMode {
        Attack = 1,
        Patrol = 2,
        Return = 3
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

    //Physics management, should move to custom function
    protected virtual void FixedUpdate() {
        HandleBehaviour();

        if (isLanded && fuel < Type.fuelCapacity) {
            if (fuel < Type.fuelCapacity) {
                fuel += Type.fuelRefillRate * Time.fixedDeltaTime;
                UpdateUISliders();

                // When fuel is completely refilled, continue the previous mode
                if (fuel > Type.fuelCapacity) {
                    fuel = Type.fuelCapacity;
                    SetMode(lastUnitMode);
                }
            }
        }

        if (PhotonView.IsMine && nextSyncTime <= Time.time + GetTimeCode()) Sync();
    }

    //Initialize the unit
    public virtual void Set(Vector2 position, Vector2 velocity, UnitType unitType, float timeCode, byte teamCode) {
        base.Set(timeCode, teamCode, true);
        PhotonView = GetComponent<PhotonView>();
        hasInventory = true;

        Type = unitType;
        transform.position = position;
        this.velocity = velocity;

        health = unitType.health;
        fuel = unitType.fuelCapacity;

        velocityCap = unitType.velocityCap;
        accel = unitType.accel;
        lowAccel = unitType.accel / 3;
        drag = unitType.drag;
        rotationSpeed = unitType.rotationSpeed;

        bankAmount = unitType.bankAmount;
        bankSpeed = unitType.bankSpeed;

        canVTOL = unitType.canVTOL;

        spriteHolder = transform.GetChild(0);
        trailRenderers = (TrailRenderer[])transform.GetComponentsInChildren<TrailRenderer>(true).Clone();
        takeOffEffect = transform.GetComponentInChildren<ParticleSystem>();

        unitUI = transform.GetComponentInChildren<UnitUI>(true);
        unitUI.ShowUI(true);

        name = "Unit Team : " + teamCode + " " + Random.Range(1, 100);
 
        SetInventory();
        SetSprites();
        SetWeapons();

        MapManager.units.Add(this);
    }

    private void SetSprites() {
        spriteHolder.GetComponent<SpriteRenderer>().sprite = Type.sprite;

        SetOptionalSprite(spriteHolder.Find("Cell"), Type.cellSprite, out SpriteRenderer teamSpriteRenderer);
        teamSpriteRenderer.color = teamCode == TeamUtilities.GetLocalPlayerTeam().Code ? TeamUtilities.LocalTeamColor : TeamUtilities.EnemyTeamColor;
    } //Set the Unit's sprites 

    private void SetWeapons() {
        foreach (WeaponMount weaponMount in Type.weapons) {
            //Get weapon prefab
            GameObject weaponPrefab = Assets.GetPrefab("weaponPrefab");

            //Create and initialize new weapon
            GameObject weaponGameObject = Instantiate(weaponPrefab, transform.position + (Vector3)weaponMount.position, Quaternion.identity, spriteHolder);
            Weapon weapon = (Weapon)weaponGameObject.AddComponent(weaponMount.weaponType.type);
            weapon.Set(this, weaponMount.weaponType, false, timeCode, teamCode);
            weapons.Add(weapon);

            //If mirrored, repeat previous steps
            if (weaponMount.mirrored) {
                weaponGameObject = Instantiate(weaponPrefab, transform.position + new Vector3(-weaponMount.position.x, weaponMount.position.y, 0), Quaternion.identity, spriteHolder);
                weapon = (Weapon)weaponGameObject.AddComponent(weaponMount.weaponType.type);
                weapon.Set(this, weaponMount.weaponType, true, timeCode, teamCode);
                weapons.Add(weapon);
            }
        }
    } //Set the Unit's weapons

    private void SetTrailTime(float time) {
        foreach (TrailRenderer tr in trailRenderers) tr.time = Mathf.Abs(time);
    } //Simulate drag trails


    #region - Behaviour -

    private float HandleFuelUse(float power) {
        fuel -= power * Type.fuelConsumption * Time.fixedDeltaTime;
        UpdateUISliders();

        // On 10s fuel, go back to land
        if (fuel / Type.fuelConsumption < 10f) SetMode(UnitMode.Return, false);

        return fuel > 0 ? (1 - (Type.fuelCapacity / (Type.fuelCapacity * 0.5f * fuel))) * power : 0f;
    }

    public void MoveTo(Vector2 position, float maxPower = 1) {
        float distance = Vector2.Distance(position, transform.position);
        float power = 1;

        //Reduce power as it gets closer
        if (distance >= 15) power = 1 - (1 / Mathf.Sqrt(distance + 1));

        //Clamp power, 0 to 1 and apply fuel things
        power = HandleFuelUse(Mathf.Clamp(power, 0, maxPower));

        //If isn't landed or taking off, calculate normal physics
        if (!isLanded && !isTakingOff) {
            RotateTowards(position, power);

            //Set the direction
            Vector2 direction = transform.up;
            float gForce = Vector2.Angle(direction, velocity.normalized) / 90;

            //Get acceleration and drag values based on direction
            Vector2 accelDir = accel * power * Time.fixedDeltaTime * direction.normalized;
            Vector2 dragDir = drag * Time.fixedDeltaTime * (Vector2.Dot(direction, velocity) * 2 + 0.5f) * -velocity.normalized;

            //Tilt the unit according to accelDot
            spriteHolder.localEulerAngles = new Vector3(0, Mathf.LerpAngle(spriteHolder.localEulerAngles.y, gForce * bankAmount, bankSpeed * Time.fixedDeltaTime), 0);

            //Set trail renderer time
            SetTrailTime(Vector2.Dot(Quaternion.AngleAxis(90, Vector3.forward) * direction, velocity));

            //Calculate the final velocity
            velocity = Vector2.ClampMagnitude(velocity + accelDir + dragDir, velocityCap);
            transform.position += (Vector3)velocity;
        }
    }

    public void HandleBehaviour() {
        //If is flying too slow, land
        if (!isLanded && !isTakingOff && velocity.magnitude < 0.05f) Land();

        //Increase or decrease unit scale on landing and takeoff
        if (isTakingOff && spriteHolder.localScale.x < 1f) spriteHolder.localScale += 0.1f * Time.fixedDeltaTime * Vector3.one;
        else if (velocity.magnitude < 0.2f) spriteHolder.localScale = Vector3.one * (0.7f + Mathf.Clamp(velocity.magnitude, 0f, 0.2f));
        

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
        }
    }

    #region - Behaviours -
    protected virtual void AttackBehaviour() {
        //If is landed, takeoff
        if (isLanded) TakeOff();
        else if (!isTakingOff) {
            //Get target
            LoadableContent target = HandleTargeting();
            Target = !target ? TeamUtilities.GetClosestCoreBlock(GetPosition(), TeamUtilities.GetEnemyTeam(teamCode).Code) : target;
            if (!Target) SetMode(UnitMode.Return);

            //Move towards target
            MoveTo(Target.GetPosition());
            HandleShooting();
        }
    }

    protected virtual void PatrolBehaviour() {
        //If is landed, takeoff
        if (isLanded) TakeOff();
        else if (!isTakingOff) {
            //Get target
            Target = HandleTargeting();
            //Target = target && !ValidTarget(target) ? null : target;

            if (!Target && (Vector2.Distance(patrolPosition, GetPosition()) < 5f || patrolPosition == Vector2.zero)) SetNewPatrolPoint();     
            MoveTo(Target ? Target.GetPosition() : patrolPosition);

            HandleShooting();
        }
    }


    protected virtual void ReturnBehaviour() {
        if (isLanded) return;

        //If target block is invalid, get closest landpad
        if (landPadSearchTime < Time.time && (!Target || !(Target is LandPadBlock) || (Target as LandPadBlock).IsFull())) {
            // Search for a landpad
            landPadSearchTime = 3f + Time.time;
            LandPadBlock targetLandPad = MapManager.Instance.GetClosestLandPad(GetPosition(), teamCode);

            // If there's no landpad, set previous target
            if (targetLandPad) Target = targetLandPad;
        }
        
        // If couldnt't find any landpads, continue as patrol mode
        if (Target is LandPadBlock) {
            LandPadBlock landPadTarget = Target as LandPadBlock;

            //If close to landpad, land
            float distance = Vector2.Distance(landPadTarget.GetPosition(), GetPosition());
            if (distance < (landPadTarget.Type.size / 2) + 0.5f && velocity.magnitude < 0.75f) Land(Target as LandPadBlock);

            //Move towards target
            MoveTo(Target.GetPosition(), 0.85f);

        } else PatrolBehaviour();
        
    }
    #endregion

    private LoadableContent HandleTargeting() {
        if (Target) {
            bool inShootRange = IsInShootRange(Target.GetPosition(), Type.fov, Type.range);
            timeSinceTargetLost = inShootRange ? 0 : timeSinceTargetLost + Time.fixedDeltaTime;
        }

        if ((Target == null && nextTargetSearchTime < Time.time) || timeSinceTargetLost > 3.5f) {
            nextTargetSearchTime = Time.time + 2f;
            timeSinceTargetLost = 0f;
            return GetTarget();
        }

        return Target;
    }

    private void HandleShooting() {
        if (Target) {
            //If is in shoot position, shoot
            bool canShoot = IsInShootRange(Target.GetPosition(), weapons[0].Type.maxDeviation, Type.range);
            if (canShoot != IsFullAuto) SetWeaponFullAuto(canShoot);
        } else {
            SetWeaponFullAuto(false);
        }
    }

    private bool ValidTarget(LoadableContent target) {
        if (!target) return false;
        return Vector2.Distance(target.GetPosition(), GetPosition()) < Type.range;
    }

    private LoadableContent GetTarget(System.Type[] priorityList = null) {
        //Default priority targets
        if (priorityList == null) priorityList = new System.Type[4] { typeof(Unit), typeof(CoreBlock), typeof(TurretBlock), typeof(LandPadBlock) };

        foreach(System.Type type in priorityList) {
            //Search the next priority type
            LoadableContent tempTarget;

            if (type == typeof(Unit)) tempTarget = MapManager.Instance.GetClosestLoadableContentInView(GetPosition(), transform.up, Type.fov, type, TeamUtilities.GetEnemyTeam(teamCode).Code);
            else tempTarget = MapManager.Instance.GetClosestLoadableContent(GetPosition(), type, TeamUtilities.GetEnemyTeam(teamCode).Code);

            //If target is valid, stop searching
            if (ValidTarget(tempTarget)) return tempTarget;
        }

        return null;
    }

    #endregion


    #region - Landing / Takeoff - 

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
    } //Land unit on the ground, if obstructed: crash, THIS CURRENTLY CRASHES(into the map) THE UNIT ALWAYS
   
    public void Land(LandPadBlock landPad) {
        //Land on landpad
        if (!landPad.Land(this)) return;
        currentLandPadBlock = landPad;

        //Set landed true and stop completely the unit
        isLanded = true;
        velocity = Vector2.zero;
        spriteHolder.localScale = Vector3.one * 0.7f;
        SetTrailTime(0);
    } //Land unit on a near landpad


    //When crash into the map
    public void MapCrash() {
        //Crash effects: TODO
        Delete();
    }


    //Take off from land
    public void TakeOff(bool sync = true) {
        if (isTakingOff || !isLanded) return;
        if (sync) PhotonView.RPC(nameof(RPC_Takeoff), RpcTarget.Others);

        //Start takeOff
        isTakingOff = true;
        isLanded = false;
        velocity = Vector2.zero;

        //Allow free movement in ±3s
        Invoke(nameof(EndTakeOff), 3f);

        //Play particle system
        takeOffEffect.Play();

        //If is landed on a landpad, takeoff from it
        if (currentLandPadBlock) currentLandPadBlock.TakeOff(this);
        currentLandPadBlock = null;
    }


    //Enable physics movement
    private void EndTakeOff() {
        //Takeoff ended, allowing free movement
        isTakingOff = false;
        spriteHolder.localScale = Vector3.one;
        velocity = lowAccel * Random.Range(0.75f, 1.25f) * transform.up;
    }
    #endregion


    #region - Math - 

    public bool IsInShootRange(Vector2 target, float fov, float range) {
        if (Vector2.Distance(target, GetPosition()) > range) return false;

        float cosAngle = Vector2.Dot((target - GetPosition()).normalized, transform.up);
        float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;

        return angle < fov;
    }

    //Smooth point to position
    protected void RotateTowards(Vector3 position, float power) {
        //Quirky quaternion stuff to make the unit rotate slowly
        Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, position - transform.position);
        desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * power * Time.fixedDeltaTime * (1 / velocity.magnitude));
    }

    //How much is the second vector pointing like other vector (0 = exact, 2 == oposite)
    public float GetForwardDotProduct(Vector2 v1, Vector2 v2, float threshold = 0.3f) => ((v2 != Vector2.zero && v1.magnitude > threshold) ? Vector2.Dot(v1.normalized, v2.normalized) : 1f) - 1;

    public Vector2 GetVelocity() => velocity;
    public float HealthPercent() => health / Type.health;
    public float FuelPercent() => fuel / Type.fuelCapacity;
    public float AmmoPercent() => 1;

    #endregion


    #region - Shooting -    
    bool IsFullAuto { get => weapons[0].isFullAuto; }

    public void Damage(float amount) {
        if (amount < health) { 
            health -= amount;
            UpdateUISliders();
        } else PhotonNetwork.Destroy(PhotonView);
    }

    public override void Delete() {
        if (gameObject.scene.isLoaded) {
            GameObject destroyEffect = Assets.GetPrefab("ExplosionEffect");
            Instantiate(destroyEffect, GetPosition(), Quaternion.identity);
        }

        MapManager.units.Remove(this);
        base.Delete();
    }

    public void SetWeaponFullAuto(bool value, int weaponIndex = -1) {
        if (weaponIndex == -1) foreach (Weapon weapon in weapons) weapon.isFullAuto = value;
        else weapons[weaponIndex].isFullAuto = value;
    }

    public void Shoot(int weaponIndex) {
        weapons[weaponIndex].Shoot();
    }

    #endregion
}