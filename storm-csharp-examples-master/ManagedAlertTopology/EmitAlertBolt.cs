using System;
using System.Collections.Generic;
using Microsoft.SCP;

namespace ManagedAlertTopology
{
    public class EmitAlertBolt : ISCPBolt
    {
        Context _context;

        double _minAlertTemp;
        double _maxAlertTemp;

        public EmitAlertBolt(Context ctx)
        {
            this._context = ctx;

            Context.Logger.Info("EmitAlertBolt: Constructor called");

            try
            {
                // set input schemas
                Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>
                {
                    {Constants.DEFAULT_STREAM_ID, new List<Type>() {typeof(double), typeof(string), typeof(string)}}
                };

                // set output schemas
                Dictionary<string, List<Type>> outputSchema = new Dictionary<string, List<Type>>
                {
                    {
                        Constants.DEFAULT_STREAM_ID,
                        new List<Type>() {typeof(string), typeof(double), typeof(string), typeof(string)}
                    }
                };

                // Declare input and output schemas
                _context.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, outputSchema));
                
                _minAlertTemp = 65;
                _maxAlertTemp = 68;

                Context.Logger.Info("EmitAlertBolt: Constructor completed");

            }
            catch (Exception ex)
            {
                Context.Logger.Error(ex.ToString());
            }
            
        }

        public void Execute(SCPTuple tuple)
        {

            try
            {
                double tempReading = tuple.GetDouble(0);
                String createDate = tuple.GetString(1);
                String deviceId = tuple.GetString(2);

                if (tempReading > _maxAlertTemp)
                {
                    _context.Emit(new Values(
                            "reading above bounds",
                            tempReading,
                            createDate,
                            deviceId
                        ));
                    Context.Logger.Info("Emitting above bounds: " + tempReading);
                }
                else if (tempReading < _minAlertTemp)
                {
                    _context.Emit(new Values(
                            "reading below bounds",
                            tempReading,
                            createDate,
                            deviceId
                        ));
                    Context.Logger.Info("Emitting below bounds: " + tempReading);
                }

                _context.Ack(tuple);
            }
            catch (Exception ex)
            {
                Context.Logger.Error(ex.ToString());
            }
        }

        public static EmitAlertBolt Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new EmitAlertBolt(ctx);
        }
    }
}