function start()
end

function takesnap(key)
	local profiler = require 'perf.profiler'
	profiler.start()
	print("Hello World! ")
	print(profiler.report())
	print("Test profiler??? ")
	print(profiler.report())
	profiler.stop()
end


--function calculation(key1, key2)
	--local memory = require 'perf.memory'

	--print("caculation the set: " .. key1 .."vs" .. key2)    
	--print(memory.comparesnap(key1, key2))
--end

function update()
end

function ondestroy()
   print("lua destroy")
end

--function luafilter(key, text)
	--local memory = require 'perf.memory'
	--print(memory.filterstr(key, text))
--end
