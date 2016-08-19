
using System;
using System.Collections.Generic;
using System.Text;

using Refine.Orm;

using Ems.Data.Common;
using Ems.Data;
using Ems.Interface;

namespace Br.Framework.WebControls
{
    public class LinkQueryServiceProxy : RemoteObjectManager, ILinkQueryService
    {
        public LinkQueryServiceProxy(ILinkQueryService queryService, bool cached, TimeSpan lifeSpan)
            : base(queryService, cached)
        {
            this.queryService = queryService;
            this.lifeSpan = lifeSpan;
            this.lastCallTime = DateTime.Now;
        }

        protected DateTime lastCallTime;
        protected TimeSpan lifeSpan;
        protected ILinkQueryService queryService;

        protected Ems.Data.IdentifiedUser user = null;
        public void Beat()
        {
            queryService.Beat();
        }

        public bool IsTimeOut
        {
            get
            {
                return DateTime.Now - lastCallTime > lifeSpan;
            }
        }

        public bool IsNearTimeOut
        {
            get
            {
                return DateTime.Now - lastCallTime > lifeSpan - TimeSpan.FromSeconds(30);
            }
        }

        public StatisticsCollection GetStatistics()
        {
            return queryService.GetStatistics();
        }

        public Query2DValueResult InnerQueryValues(Stack<QueryCallTrace> callTraces, ExtIndicID[] indicIDs, PeriodList periods)
        {
            return queryService.InnerQueryValues(callTraces, indicIDs, periods);
        }

        public Query3DValueResult QueryValues(Guid[] linkIDs, ExtIndicID[] indicIDs, PeriodList periods)
        {
            return queryService.QueryValues(linkIDs, indicIDs, periods);
        }

        public Query2DValueResult QueryValues(ExtIndicID[] indicIDs, PeriodList periods)
        {
            return queryService.QueryValues(indicIDs, periods);
        }
    }
}
