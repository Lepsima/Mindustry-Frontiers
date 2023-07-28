using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CI.QuickSave.Core;
using Newtonsoft.Json;
using Frontiers.Assets;

namespace Frontiers.Content.Sounds {
    public static class SoundHandler {
        public static Dictionary<string, Sound> loadedSounds = new();
        
        public static void Handle(Sound sound) {
            sound.id =(short)loadedSounds.Count;
            loadedSounds.Add(sound.name, sound);
        }
    }

    public class Sound {
        [JsonIgnore] public AudioClip clip;
        public string name;
        public short id;

        public Sound(string name) {
            this.name = name;
            clip = AssetLoader.GetAsset<AudioClip>(name);
            SoundHandler.Handle(this);
        }
    }

    public static class SoundTypes {
        public static Sound
            artillery, bang, beam, bigShot, bioLoop, blaster, bolt, boom, @break, build, buttonClick, cannon, click, combustion,
            conveyor, copterBladeLoop, coreExplode, cutter, door, drill, drillCharge, drillImpact, dullExplosion, electricHum,
            explosion, explosionBig, extractLoop, fire, flame, flame2, flux, glow, grinding, hum, largeCannon, largeExplosion,
            laser, laserBeam, laserBig, laserBlast, laserCharge, laserCharge2, laserShoot, machine, malignShoot, mineBeam, mineDeploy,
            missile, missileLarge, missileLaunch, missileTrail, mud, noAmmo, pew, place, plantBreak, plasmaBoom, plasmaDrop, pulse,
            pulseBlast, railgun, rain, release, respawn, respawning, rockBreak, sap, shield, shockBlast, shoot, shootAlt,
            shootAltLong, shootBig, shootSmite, shootSnap, shotgun, smelter, spark, spellLoop, splash, spray, steam, swish, techLoop,
            thruster, titanExplosion, torch, tractorBeam, wave, wind, wind2, wind3, windHowl;

        // https://processing.org Processing Script to auto generate all sound constructors 
        // Copy consoloe log to get all the generated constructors

        /*
        String names = "name1, name2, name3, name4, etc";
        String[] allNames;

        String constructor = "NAME = new(\"NAME\");";

        void setup() {
            allNames = names.split(",");

            for (int i = 0; i < allNames.length; i++) {
                allNames[i] = allNames[i].trim();
                String full = constructor.replaceAll("NAME", allNames[i]);
                println(full);
            }
        }
        */
        public static void Load() {
            artillery = new("artillery");
            bang = new("bang");
            beam = new("beam");
            bigShot = new("bigShot");
            bioLoop = new("bioLoop");
            blaster = new("blaster");
            bolt = new("bolt");
            boom = new("boom");
            @break = new("break");
            build = new("build");
            buttonClick = new("buttonClick");
            cannon = new("cannon");
            click = new("click");
            combustion = new("combustion");
            conveyor = new("conveyor");
            copterBladeLoop = new("copterBladeLoop");
            coreExplode = new("coreExplode");
            cutter = new("cutter");
            door = new("door");
            drill = new("drill");
            drillCharge = new("drillCharge");
            drillImpact = new("drillImpact");
            dullExplosion = new("dullExplosion");
            electricHum = new("electricHum");
            explosion = new("explosion");
            explosionBig = new("explosionBig");
            extractLoop = new("extractLoop");
            fire = new("fire");
            flame = new("flame");
            flame2 = new("flame2");
            flux = new("flux");
            glow = new("glow");
            grinding = new("grinding");
            hum = new("hum");
            largeCannon = new("largeCannon");
            largeExplosion = new("largeExplosion");
            laser = new("laser");
            laserBeam = new("laserBeam");
            laserBig = new("laserBig");
            laserBlast = new("laserBlast");
            laserCharge = new("laserCharge");
            laserCharge2 = new("laserCharge2");
            laserShoot = new("laserShoot");
            machine = new("machine");
            malignShoot = new("malignShoot");
            mineBeam = new("mineBeam");
            mineDeploy = new("mineDeploy");
            missile = new("missile");
            missileLarge = new("missileLarge");
            missileLaunch = new("missileLaunch");
            missileTrail = new("missileTrail");
            mud = new("mud");
            noAmmo = new("noAmmo");
            pew = new("pew");
            place = new("place");
            plantBreak = new("plantBreak");
            plasmaBoom = new("plasmaBoom");
            plasmaDrop = new("plasmaDrop");
            pulse = new("pulse");
            pulseBlast = new("pulseBlast");
            railgun = new("railgun");
            rain = new("rain");
            release = new("release");
            respawn = new("respawn");
            respawning = new("respawning");
            rockBreak = new("rockBreak");
            sap = new("sap");
            shield = new("shield");
            shockBlast = new("shockBlast");
            shoot = new("shoot");
            shootAlt = new("shootAlt");
            shootAltLong = new("shootAltLong");
            shootBig = new("shootBig");
            shootSmite = new("shootSmite");
            shootSnap = new("shootSnap");
            shotgun = new("shotgun");
            smelter = new("smelter");
            spark = new("spark");
            spellLoop = new("spellLoop");
            splash = new("splash");
            spray = new("spray");
            steam = new("steam");
            swish = new("swish");
            techLoop = new("techLoop");
            thruster = new("thruster");
            titanExplosion = new("titanExplosion");
            torch = new("torch");
            tractorBeam = new("tractorBeam");
            wave = new("wave");
            wind = new("wind");
            wind2 = new("wind2");
            wind3 = new("wind3");
            windHowl = new("windHowl");
        }
    }
}
