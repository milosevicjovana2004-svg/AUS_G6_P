using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;

            byte[] request = new byte[12];

            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.TransactionId)), 0, request, 0, 2);
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.ProtocolId)), 0, request, 2, 2);
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Length)), 0, request, 4, 2);

            request[6] = p.UnitId;
            request[7] = p.FunctionCode;

            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.StartAddress)), 0, request, 8, 2);
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)p.Quantity)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            if (response == null || response.Length < 9)
            {
                throw new ArgumentException("Invalid response.");
            }

            byte functionCode = response[7];
            if ((functionCode & 0x80) != 0)
            {
                HandeException(response[8]);
            }

            ModbusReadCommandParameters p = (ModbusReadCommandParameters)CommandParameters;
            Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            ushort startAddress = p.StartAddress;
            ushort quantity = p.Quantity;

            for (int i = 0; i < quantity; i++)
            {
                int index = 9 + i * 2;
                if (index + 1 >= response.Length)
                {
                    break;
                }

                ushort value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, index));
                result.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, (ushort)(startAddress + i)), value);
            }

            return result;
        }
    }
}