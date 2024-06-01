local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")

local RailBoosterNode = {}
RailBoosterNode.name = "VerUtils/RailBooster-Node"
RailBoosterNode.depth = -25500
RailBoosterNode.placements = {
    {
        name = "normal",
        data = {
            isEntry = true,
            instant = false
        }
    },
    {
        name = "instantFork",
        data = {
            isEntry = false,
            instant = true
        }
    }
}
RailBoosterNode.fieldInformation = {
    isEntry = {
        fieldType = "boolean"
    },
    instant = {
        fieldType = "boolean"
    }
}
function RailBoosterNode.selection(room, entity)
    local selections
    if entity.isEntry then
        selections = utils.rectangle(entity.x-9, entity.y-9, 18, 18)
    else
        selections = utils.rectangle(entity.x-6, entity.y-6, 12, 12)
    end
    return selections, {}
end
function RailBoosterNode.sprite(room, entity)
    local texture = drawableSprite.fromTexture(entity.isEntry and "objects/VerUtils/RailBooster/node0000" or "objects/VerUtils/RailBooster/node0029")
    texture:setJustification(0.5, 0.5)
    texture:setPosition(entity.x, entity.y)
    return texture
end
return RailBoosterNode;