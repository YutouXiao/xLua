-- Tencent is pleased to support the open source community by making xLua available.
-- Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
-- Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
-- http://opensource.org/licenses/MIT
-- Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.

function start()
end

function takesnap(key)
	local memory = require 'perf.memory'

	print('total memory:', memory.total())
	print("takesnapshot  " .. key)
	print(memory.takesnap(key))
end

function calculation(key1, key2)
	local memory = require 'perf.memory'

	print("caculation the set: " .. key1 .."vs" .. key2)    
	print(memory.comparesnap(key1, key2))
end

function update()
end

function ondestroy()
    print("lua destroy")
end

function luafilter(key, text)
	local memory = require 'perf.memory'
	print(memory.filterstr(key, text))
end


