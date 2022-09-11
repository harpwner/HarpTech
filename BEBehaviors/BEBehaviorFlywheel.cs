﻿using HarpTech.BlockEntities;
using HarpTech.Mechanics;
using HarpTech.Renderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace HarpTech.BEBehaviors
{
    class BEBehaviorFlywheel : BEBehaviorMPRotor
    {
        FlywheelArmRenderer rendererArm;
        FlywheelGearRenderer rendererGear;
        BlockFacing face;

        protected override float Resistance => 0.6f;
        protected override double AccelerationFactor => 0.06d;
        protected override float TorqueFactor => GetEfficiency() * 8;
        protected override float TargetSpeed => GetEfficiency() / 4f;

        public BEBehaviorFlywheel(BlockEntity blockentity) : base(blockentity) { }

        public override float AngleRad => GetAngleRad();

        float GetAngleRad()
        {
            if (network == null) return lastKnownAngleRad;

            if (isRotationReversed())
            {
                return (lastKnownAngleRad = (GameMath.TWOPI * 2) - (network.AngleRad * this.GearedRatio) % (GameMath.TWOPI * 2));
            }

            return (lastKnownAngleRad = (network.AngleRad * this.GearedRatio) % (GameMath.TWOPI * 2));
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            face = BlockFacing.FromCode(Block.Variant["side"]);

            if (api.Side == EnumAppSide.Client)
            {
                rendererArm = new FlywheelArmRenderer(api as ICoreClientAPI, this.Blockentity.Pos, GetArmMesh(), this);
                rendererArm.ShouldRender = true;
                (api as ICoreClientAPI).Event.RegisterRenderer(rendererArm, EnumRenderStage.Opaque, "wattarm");

                rendererGear = new FlywheelGearRenderer(api as ICoreClientAPI, this.Blockentity.Pos, GetGearMesh(), this);
                rendererGear.ShouldRender = true;
                (api as ICoreClientAPI).Event.RegisterRenderer(rendererGear, EnumRenderStage.Opaque, "wattgear");
            }
        }

        private MeshData GetArmMesh()
        {
            Block block = Api.World.BlockAccessor.GetBlock(this.Blockentity.Pos);
            if (block.BlockId == 0) return null;

            MeshData mesh;
            ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

            mesher.TesselateShape(block, Vintagestory.API.Common.Shape.TryGet(Api, "harptech:shapes/block/wattengine/wheel_arm.json"), out mesh);

            return mesh;
        }

        private MeshData GetGearMesh()
        {
            Block block = Api.World.BlockAccessor.GetBlock(this.Blockentity.Pos);
            if (block.BlockId == 0) return null;

            MeshData mesh;
            ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

            mesher.TesselateShape(block, Vintagestory.API.Common.Shape.TryGet(Api, "harptech:shapes/block/wattengine/flywheel_arm_gear.json"), out mesh);

            return mesh;
        }

        public int[] GetRendModifiers()
        {
            int[] modifiers = new int[4] { 0, 0, 0, 0 };

            switch (Block.Variant["side"])
            {
                case "north":
                    modifiers = new int[4] { 1, -1, 0, 180 };
                    break;
                case "south":
                    modifiers = new int[4] { 1, -1, 0, 0 };
                    break;
                case "west":
                    modifiers = new int[4] { 0, -1, -1, 270 };
                    break;
                case "east":
                    modifiers = new int[4] { 0, 1, 1, 90 };
                    break;
            }

            return modifiers;
        }

        public int GetRotDir()
        {
            switch (Block.Variant["side"])
            {
                //for some ungodly reason, mech power facing west causes inconsistent rotation with other directions
                case "west":
                    return -1;
            }

            return 1;
        }

        public override void OnBlockRemoved()
        {
            rendererArm?.Dispose();
            rendererArm = null;

            rendererGear?.Dispose();
            rendererGear = null;

            base.OnBlockRemoved();
        }

        public override void OnBlockUnloaded()
        {
            rendererArm?.Dispose();
            rendererArm = null;

            rendererGear?.Dispose();
            rendererGear = null;

            base.OnBlockUnloaded();
        }

        float GetEfficiency()
        {
            BEPiston piston = GetPiston();
            if (piston != null && HasLeverArm())
            {
                return piston.Pressure;
            } else
            {
                return 0;
            }
        }

        bool HasLeverArm()
        {
            BlockFacing cw = this.face.GetCW();
            BlockFacing ccw = this.face.GetCCW();

            BlockPos searchPos = this.Position.Copy();
            BlockPos searchPos2 = this.Position.Copy();

            searchPos.Add(cw).Add(cw).Add(BlockFacing.UP).Add(BlockFacing.UP);
            searchPos2.Add(ccw).Add(ccw).Add(BlockFacing.UP).Add(BlockFacing.UP);

            BEBehaviorLeverArm lever = Api.World.BlockAccessor.GetBlockEntity(searchPos)?.GetBehavior<BEBehaviorLeverArm>();
            if(lever == null)
            {
                lever = Api.World.BlockAccessor.GetBlockEntity(searchPos2)?.GetBehavior<BEBehaviorLeverArm>();
            }

            return lever != null;
        }

        BEPiston GetPiston()
        {
            BlockFacing cw = this.face.GetCW();
            BlockFacing ccw = this.face.GetCCW();

            BlockPos searchPos = this.Position.Copy();
            BlockPos searchPos2 = this.Position.Copy();

            searchPos.Add(cw).Add(cw).Add(cw).Add(cw).Add(BlockFacing.UP);
            searchPos2.Add(ccw).Add(ccw).Add(ccw).Add(ccw).Add(BlockFacing.UP);

            BEPiston piston = Api.World.BlockAccessor.GetBlockEntity(searchPos) as BEPiston;
            if(piston == null)
            {
                piston = Api.World.BlockAccessor.GetBlockEntity(searchPos2) as BEPiston;
            }

            return piston == null ? null : piston;
        }
    }
}
