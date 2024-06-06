local utils = require("utils")
local viewportHandler = require("viewport_handler")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")

local shifter = {}

shifter.name = "VerUtils/Shifter"
shifter.depth = 0
shifter.color = {0.87, 0.71, 0.32, 0.5}

local cachedArrow = nil

function drawArrow(x1, y1, x2, y2, len, angle)
	love.graphics.line(x1, y1, x2, y2)
	local a = math.atan2(y1 - y2, x1 - x2)
	love.graphics.line(x2, y2, x2 + len * math.cos(a + angle), y2 + len * math.sin(a + angle))
	love.graphics.line(x2, y2, x2 + len * math.cos(a - angle), y2 + len * math.sin(a - angle))
end


shifter.placements = {
    name = "normal",
    data = {
        width = 8,
        height = 8,
        SpeedX = 0,
        SpeedY = 0
    }
}

function checkCursorBounds(ex,ey,width,height,room)
    local mx, my = viewportHandler.getMousePosition()
    local x,y = viewportHandler.getRoomCoordinates(room, mx, my)

    if (x>=ex and x<=ex+width) and (y>=ey and y<=ey+height) then
        return true
    end

    return false
end

function shifter.draw(room, entity, viewport)
    local rect = drawableRectangle.fromRectangle("fill", entity.x, entity.y, entity.width, entity.height)
    local rect2 = drawableRectangle.fromRectangle("line", entity.x, entity.y, entity.width, entity.height)
    rect.color = shifter.color
    rect2.color = rect.color

    local bounded = checkCursorBounds(rect.x, rect.y, rect.width, rect.height, room)

    rect:draw()
    rect2:draw()

    local arrow = drawableSprite.fromTexture("loenn/shifterArrow","Gameplay")
    arrow.depth = 0

    arrow.x = entity.x + entity.width/2
    arrow.y = entity.y + entity.height/2

    local speed = {entity.SpeedX, entity.SpeedY}

    if not (speed[1]==0 and speed[2]==0) then
        arrow.rotation = (math.atan2(entity.SpeedY, entity.SpeedX))
    end

    if bounded and arrow then
        arrow:draw()
    end
end

return shifter
