using System;
using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.Physics
{
    public enum CollisionType
    {
        NoCollision,
        QueryOnly,
        PhysicsOnly,
        Collision
    }

    public enum CollisionObjectType
    {
        WorldDynamic,
        WorldStatic,
        Pawn,
        PhysicsBody,
    }

    public enum CollisionResponseTypes
    {
        Ignore,
        Overlap,
        Block
    }

    public interface ICollisionPreset
    {
        public CollisionType CollisionEnabled { get; set; }
        public CollisionObjectType ObjectType { get; set; }
        public CollisionResponses CollisionResponses { get; set; }
    }

    public struct CollisionResponses
    {
        [Display(0, "TraceResponse")]
        public CollisionTraceResponse TraceResponse;

        [Display(1, "ObjectResponse")]
        public CollisionObjectResponse ObjectResponse;
    }

    /*
    [DataContract("bool3")]
    [DataStyle(DataStyle.Compact)]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CollisionResponseTypes : IEquatable<CollisionResponseTypes>, IFormattable
    {
        [DataMember(0)]
        public bool Ignore;

        [DataMember(1)]
        public bool Overlap;

        [DataMember(2)]
        public bool Block;

        public CollisionResponseTypes(bool ignore, bool overlap, bool block)
        {
            Ignore = ignore;
            Overlap = overlap;
            Block = block;
        }

        public bool Equals(CollisionResponseTypes other)
        {
            return Ignore == other.Ignore &&
                   Overlap == other.Overlap && 
                   Block == other.Block;
        }

        public override bool Equals(object value)
        {
            if (value == null)
                return false;

            if (value.GetType() != GetType())
                return false;

            return Equals((CollisionResponseTypes)value);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return $"Ignore:{Ignore} Overlap:{Overlap} Block:{Block}";
        }

        public override string ToString()
        {
            return $"Ignore:{Ignore} Overlap:{Overlap} Block:{Block}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ignore, Overlap, Block);
        }
    }*/

    public struct CollisionTraceResponse
    {
        [Display(0, "Visibility")]
        public CollisionResponseTypes Visibility { get; set; }

        [Display(1, "Camera")]
        public CollisionResponseTypes Camera { get; set; }
    }

    public struct CollisionObjectResponse
    {
        [Display(0, "WorldStatic")]
        public CollisionResponseTypes WorldStatic { get; set; }

        [Display(1, "WorldDynamic")]
        public CollisionResponseTypes WorldDynamic { get; set; }

        [Display(2, "Pawn")]
        public CollisionResponseTypes Pawn { get; set; }

        [Display(3, "PhysicsBody")]
        public CollisionResponseTypes PhysicsBody { get; set; }
    }

    [DataContract("CollisionPresetDefault")]
    [Display(0, "Default")]
    public class CollisionPresetDefault : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.PhysicsOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldStatic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses 
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Block,
                    Visibility = CollisionResponseTypes.Block
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Block,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { } 
        }
    }

    [DataContract("CollisionPresetCustom")]
    [Display(1, "Custom")]
    public class CollisionPresetCustom : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get; set; }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get; set; }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses { get; set; }
    }

    [DataContract("CollisionPresetNoCollision")]
    [Display(2, "NoCollision")]
    public class CollisionPresetNoCollision : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.NoCollision; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldStatic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Ignore,
                    Visibility = CollisionResponseTypes.Ignore
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Ignore,
                    PhysicsBody = CollisionResponseTypes.Ignore,
                    WorldDynamic = CollisionResponseTypes.Ignore,
                    WorldStatic = CollisionResponseTypes.Ignore
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetBlockAll")]
    [Display(3, "BlockAll")]
    public class CollisionPresetBlockAll : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.Collision; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldStatic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Block,
                    Visibility = CollisionResponseTypes.Block
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Block,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetOverlapAll")]
    [Display(4, "OverlapAll")]
    public class CollisionPresetOverlapAll : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.QueryOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldStatic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Overlap,
                    Visibility = CollisionResponseTypes.Overlap
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Overlap,
                    PhysicsBody = CollisionResponseTypes.Overlap,
                    WorldDynamic = CollisionResponseTypes.Overlap,
                    WorldStatic = CollisionResponseTypes.Overlap
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetBlockAllDynamic")]
    [Display(5, "BlockAllDynamic")]
    public class CollisionPresetOverlapBlockAllDynamic : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.Collision; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldDynamic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Block,
                    Visibility = CollisionResponseTypes.Block
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Block,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetOverlapAllDynamic")]
    [Display(6, "OverlapAllDynamic")]
    public class CollisionPresetOverlapAllDynamic : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.QueryOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldStatic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Overlap,
                    Visibility = CollisionResponseTypes.Overlap
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Overlap,
                    PhysicsBody = CollisionResponseTypes.Overlap,
                    WorldDynamic = CollisionResponseTypes.Overlap,
                    WorldStatic = CollisionResponseTypes.Overlap
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetIgnoreOnlyPawn")]
    [Display(7, "IgnoreOnlyPawn")]
    public class CollisionPresetIgnoreOnlyPawn : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.QueryOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldDynamic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Block,
                    Visibility = CollisionResponseTypes.Block
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Ignore,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetOverlapOnlyPawn")]
    [Display(8, "OverlapOnlyPawn")]
    public class CollisionPresetOverlapOnlyPawn : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.QueryOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldDynamic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Block,
                    Visibility = CollisionResponseTypes.Block
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Overlap,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetPawn")]
    [Display(9, "Pawn")]
    public class CollisionPresetPawn : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.Collision; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.Pawn; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Ignore,
                    Visibility = CollisionResponseTypes.Block
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Block,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetSpectator")]
    [Display(10, "Spectator")]
    public class CollisionPresetSpectator : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.QueryOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.Pawn; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Ignore,
                    Visibility = CollisionResponseTypes.Ignore
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Ignore,
                    PhysicsBody = CollisionResponseTypes.Ignore,
                    WorldDynamic = CollisionResponseTypes.Ignore,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetCharacterMesh")]
    [Display(11, "CharacterMesh")]
    public class CollisionPresetCharacterMesh : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.QueryOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.Pawn; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Block,
                    Visibility = CollisionResponseTypes.Ignore
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Ignore,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetPhysicsActor")]
    [Display(12, "PhysicsActor")]
    public class CollisionPresetPhysicsActor : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.Collision; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.PhysicsBody; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Block,
                    Visibility = CollisionResponseTypes.Block
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Block,
                    PhysicsBody = CollisionResponseTypes.Block,
                    WorldDynamic = CollisionResponseTypes.Block,
                    WorldStatic = CollisionResponseTypes.Block
                }
            };
            set { }
        }
    }

    [DataContract("CollisionPresetTrigger")]
    [Display(13, "Trigger")]
    public class CollisionPresetTriggerr : ICollisionPreset
    {
        [Display(0, "CollisionEnabled")]
        public CollisionType CollisionEnabled { get => CollisionType.QueryOnly; set { } }

        [Display(1, "ObjectType")]
        public CollisionObjectType ObjectType { get => CollisionObjectType.WorldDynamic; set { } }

        [Display(2, "CollisionResponses")]
        public CollisionResponses CollisionResponses
        {
            get => new CollisionResponses
            {
                TraceResponse = new CollisionTraceResponse
                {
                    Camera = CollisionResponseTypes.Overlap,
                    Visibility = CollisionResponseTypes.Ignore
                },
                ObjectResponse = new CollisionObjectResponse
                {
                    Pawn = CollisionResponseTypes.Overlap,
                    PhysicsBody = CollisionResponseTypes.Overlap,
                    WorldDynamic = CollisionResponseTypes.Overlap,
                    WorldStatic = CollisionResponseTypes.Overlap
                }
            };
            set { }
        }
    }
}
