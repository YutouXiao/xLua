-- Tencent is pleased to support the open source community by making xLua available.
-- Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
-- Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
-- http://opensource.org/licenses/MIT
-- Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.

local get_time = os.clock
local sethook = xlua.sethook or debug.sethook
local func_info_map = nil

local start_time

local function string_format_report(rp)
    local size = string.format("%-7i", rp.size)
    return string.lower(rp.name), size, TYPE_NAME[rp.type], rp.pointer, rp.used_in
end

local function create_func_info(db_info)
    return {
		db_info = db_info,
		count = 0,
		total_time = 0
	}
end

local function on_hook(event, func_info_id, source)
    local func_info = func_info_map[func_info_id]
    if not func_info then
        func_info = create_func_info(debug.getinfo( 2, 'nS' ))
        func_info_map[func_info_id] = func_info
    end
	if event == "call" then
		func_info.call_time = get_time()
        func_info.count = func_info.count + 1
        func_info.return_time = nil
	elseif event == "return" or event == 'tail return' then
        local now = get_time()
        if func_info.call_time then
            func_info.total_time = func_info.total_time + (now - func_info.call_time)
            func_info.call_time = nil
        else
            func_info.total_time = func_info.total_time + (now - (func_info.return_time or now))
            func_info.count = func_info.count + 1
        end
        func_info.return_time = now
        if source and func_info.count == 1 then
            func_info.db_info.short_src = source
        end
	end
end

function start()
    func_info_map = {}
    start_time = get_time()
    sethook(on_hook, 'cr')
end

local function pause()
    sethook()
end

local function resume()
    sethook(on_hook, 'cr')
end

function stop()
    sethook()
    func_info_map = nil
    start_time = nil
end

local function report_output_line(rp, stat_interval)
    local source        = rp.db_info.short_src or '[NA]'
    local linedefined   = (rp.db_info.linedefined and rp.db_info.linedefined >= 0) and string.format(":%i", rp.db_info.linedefined) or ''
    source = source .. linedefined
    local name          = rp.db_info.name or '[anonymous]'
    local total_time    = string.format("%f", rp.total_time * 1000)
    local average_time    = string.format("%f", rp.total_time / rp.count * 1000)
    local relative_time = string.format("%f%%", (rp.total_time / stat_interval) * 100 )
    local count         = string.format("%7i", rp.count)
        
    return string.format("\n%-40s: %-30s: %-12s: %-12s: %-12s: %s\n", string.lower(name), source, total_time, average_time, relative_time, count)

    -- return string.lower(name), source, total_time, average_time, relative_time, count
end

local sort_funcs = {
    TOTAL = function(a, b) return a.total_time > b.total_time end,
    AVERAGE = function(a, b) return a.average > b.average end,
    CALLED = function(a, b) return a.count > b.count end
}

function report(sort_by)
    print("111")
    sethook()
    local sort_func = type(sort_by) == 'function' and sort_by or sort_funcs[sort_by]
    
    local output = {}
    output[1] = {func = "FUNCTION", source = "SOURCE", total = "TOTAL(MS)", average = "AVERAGE(MS)", relative = "RELATIVE", called = "CALLED"}
    
    local stat_interval = get_time() - (start_time or get_time())
    
    local report_list = {}
    for k, rp in pairs(func_info_map) do
        table.insert(report_list, {
            total_time = rp.total_time,
            count = rp.count,
            average = rp.total_time / rp.count,
            output = report_output_line(rp, stat_interval)
        })
    end
    
    table.sort(report_list, sort_func or sort_funcs.TOTAL)
    
    -- for i, rp in ipairs(report_list) do
    --     output[i+1] = {}
    --     output[i+1] = rp.output
    -- end
    
    sethook(on_hook, 'cr')
    
    return output
end

-- output[k+1]["func"], output[k+1]["source"], output[k+1]["total"], output[k+1]["average"], output[k+1]["relative"], output[k+1]["called"] =

return {
    --开始统计
    start = start,
    --获取报告，start和stop之间可以多次调用，参数sort_by类型是string，可以是'TOTAL','AVERAGE', 'CALLED'
    report = report,
    --停止统计
    stop = stop
}
