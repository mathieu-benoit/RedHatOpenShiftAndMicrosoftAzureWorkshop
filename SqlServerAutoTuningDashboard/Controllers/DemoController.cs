using Belgrade.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace SqlServerAutoTuningDashboard.Controllers
{
    [Route("api/[controller]")]
    public class DemoController : Controller
    {
        IQueryMapper QueryMapper = null;

        public DemoController(IQueryMapper queryMapper)
        {
            this.QueryMapper = queryMapper;
        }

        // GET api/demo
        [HttpGet]
        [Produces("application/json")]
        public async Task<long> Get()
        {
            decimal result = 0;
            string status = "OK";
            long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long end = 0;
            await QueryMapper
                .OnError(ex => status = ex.Message)
                .ExecuteReader("EXEC dbo.report 7", reader => {
                    result = reader.GetDecimal(0);
                    end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                });
            //return "{\"x\":\"" + DateTime.Now.ToUniversalTime().ToString() + "\",\"y\":" + (end - start) + ",\"start\":" + start + ",\"end\":" + end + ",\"result\":" + result + ",\"status\":\"" + status + "\"}";
            return end - start;
        }

        // GET api/demo/init
        [HttpGet("init")]
        public async Task Init()
        {
            await QueryMapper.ExecuteReader("EXEC dbo.[initialize]", _ => { });
        }

        // GET api/demo/regression
        [HttpGet("regression")]
        public async Task Regression()
        {
            await QueryMapper.ExecuteReader("EXEC dbo.regression", _ => { });
        }

        // GET api/demo/on
        [HttpGet("on")]
        public async Task On()
        {
            await QueryMapper.ExecuteReader("EXEC dbo.auto_tuning_on", _ => { });
        }


        // GET api/demo/off
        [HttpGet("off")]
        public async Task Off()
        {
            await QueryMapper.ExecuteReader("EXEC dbo.auto_tuning_off", _ => { });
        }
    }
}