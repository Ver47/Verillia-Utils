local drawing = require("utils.drawing")
local utils = require("utils")
local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")

local RailBoosterRope = {}
RailBoosterRope.name = "VerUtils/RailBooster-Rail"
RailBoosterRope.depth = -25499
RailBoosterRope.justification = {0.5, 0.5}
RailBoosterRope.placements = {
    {
        name = "normal",
        data = {
                slough = 0,
                invincible = true,
                priority = 0,
                bg = false
            }
    }
}
RailBoosterRope.fieldInformation = {
    slough = {
        fieldType = "integer",
        minimumValue = 0
    },
    invincible = {
        fieldType = "boolean"
    },
    priority = {
        fieldType = "integer"
    },
    bg = {
        fieldType = "boolean"
    }
}
RailBoosterRope.nodeLimits = {1, 1}
RailBoosterRope.nodeVisibility = "always"

local function Rail(entity, offsetx, offsety, color)
    
    local drop = entity.slough or 0

    local start = {entity.x + offsetx, entity.y + offsety}
    local stop = {entity.nodes[1].x + offsetx, entity.nodes[1].y + offsety}
    local control = {(entity.nodes[1].x+entity.x)/2 + offsetx, (entity.nodes[1].y+entity.y)/2 + offsety + drop}

    local points = drawing.getSimpleCurve(start, stop, control)
    local curveSprite = drawableLine.fromPoints(points, color, 3)

    return curveSprite
end

local function Node(X, Y)
   local texture = drawableSprite.fromTexture("objects/VerUtils/RailBooster/loennRailRopeAlignment")
   texture:setJustification(0.5, 0.5)
   texture:setPosition(X, Y)
   return texture
end

function RailBoosterRope.sprite(room, entity)
    local toreturn = {}

    -- Curve
    -- Yes, this is yandere coded. Fuck you.
    -- it is apparently only run once anyways
    -- the actual code in the game is better.
    table.insert(toreturn, Rail(entity,-1,0,"e6c1ec"))
    table.insert(toreturn, Rail(entity,1,0,"e6c1ec"))
    table.insert(toreturn, Rail(entity,-1,1,"e6c1ec"))
    table.insert(toreturn, Rail(entity,1,1,"e6c1ec"))
    table.insert(toreturn, Rail(entity,0,2,"e6c1ec"))
    table.insert(toreturn, Rail(entity,0,-1,"e6c1ec"))
    table.insert(toreturn, Rail(entity,0,1,"563b85"))
    table.insert(toreturn, Rail(entity,0,0,"a986d3"))

    -- Edges
    table.insert(toreturn, Node(entity.x, entity.y))
    table.insert(toreturn, Node(entity.nodes[1].x, entity.nodes[1].y))

    return toreturn
end

local function NodeSelect(x, y)
    return utils.rectangle(x-7, y-7, 14, 14)
end

function RailBoosterRope.selection(room, entity)
    return NodeSelect(entity.x, entity.y), {NodeSelect(entity.nodes[1].x, entity.nodes[1].y)}
end

return RailBoosterRope