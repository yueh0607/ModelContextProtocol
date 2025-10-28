using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol
{
    /// <summary>
    /// 提供在 MCP 场景下使用 Newtonsoft.Json 的统一序列化设置与辅助方法。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>与原版 System.Text.Json（含 Source Generation）实现的差异 / 损失：</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><b>无 AOT/裁剪友好</b>：不再依赖 <c>JsonSerializerContext</c> 与 <c>TypeInfoResolver</c>；
    ///     Newtonsoft.Json 基于运行时反射，Native AOT/裁剪场景不可用或需要额外保留反射信息。</description>
    ///   </item>
    ///   <item>
    ///     <description><b>性能与内存</b>：丢失 STJ 的源生成与 <c>Utf8JsonReader</c> 的零反射/低分配优势；
    ///     本实现多处使用 <c>JToken/JObject</c> 简化多态与动态结构处理，易读但分配更多、GC 压力更高。</description>
    ///   </item>
    ///   <item>
    ///     <description><b>错误发现时机</b>：原版缺少类型元数据会在编译期/启动期暴露；
    ///     现在多为运行时（字段缺失/类型不匹配/转换器未注册）才抛出序列化异常。</description>
    ///   </item>
    ///   <item>
    ///     <description><b>多态/元数据规则差异</b>：不再依赖 STJ 的 <c>AllowOutOfOrderMetadataProperties</c> 与内置多态规则；
    ///     多态判定逻辑改由自定义 <c>JsonConverter</c> 完成（基于 <c>JObject</c>），字段顺序不敏感，但兼容性语义由项目代码自行保证。</description>
    ///   </item>
    ///   <item>
    ///     <description><b>类型生态差异</b>：STJ 原生支持的若干新类型（如 <c>DateOnly/TimeOnly/Half/Int128</c> 等）
    ///     在低版本/Json.NET 下需自定义转换器或退化为字符串表示。</description>
    ///   </item>
    ///   <item>
    ///     <description><b>枚举映射</b>：原版使用 <c>JsonStringEnumMemberName</c>；本实现统一使用
    ///     <c>StringEnumConverter</c> + <c>[EnumMember(Value="...")]</c>。若遗漏 <c>EnumMember</c>，序列化字符串可能与原版不一致。</description>
    ///   </item>
    ///   <item>
    ///     <description><b>配置不可变性</b>：无 <c>options.MakeReadOnly()</c> 概念；统一通过
    ///     <c>JsonSerializerSettings</c> 管理约定（如忽略空值、命名策略、时间/数字解析等）。</description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <b>缓解与建议：</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>集中使用 <see cref="DefaultSettings"/> / <see cref="DefaultSerializer"/>，在此注册所有项目级 <c>JsonConverter</c>，统一 Null/命名/日期/数字策略。</description>
    ///   </item>
    ///   <item>
    ///     <description>为关键模型编写“序列化↔反序列化回环”单元测试，覆盖必填字段、枚举映射、多态分支与 Schema 校验。</description>
    ///   </item>
    ///   <item>
    ///     <description>若遇到性能热点，再为特定类型编写基于 <c>JsonReader/JsonWriter</c> 的“流式”转换器以替代 <c>JObject</c> 方案。</description>
    ///   </item>
    ///   <item>
    ///     <description>对 <c>TimeSpan</c>、<c>DateTimeOffset</c> 等明确格式约定（必要时提供自定义转换器），避免不同默认格式带来不兼容。</description>
    ///   </item>
    /// </list>
    /// </remarks>

    public static partial class McpJsonUtilities
    {
        /// <summary>
        /// 获取在 JSON 序列化操作中用作默认值的 <see cref="JsonSerializerSettings"/> 单例。
        /// </summary>
        public static JsonSerializerSettings DefaultSettings { get; } = CreateDefaultSettings();
    
        /// <summary>
        /// 基于 <see cref="DefaultSettings"/> 创建的默认 Json.NET 序列化器。
        /// </summary>
        public static readonly JsonSerializer DefaultSerializer = JsonSerializer.Create(DefaultSettings);

        /// <summary>
        /// MCP 工具输入/输出的默认 JSON Schema：{ "type": "object" }。
        /// </summary>
        public static readonly JObject DefaultMcpToolSchema = new JObject { ["type"] = "object" };

        /// <summary>
        /// 创建用于 MCP 相关序列化的默认选项。
        /// </summary>
        /// <returns>已配置的选项。</returns>
        private static JsonSerializerSettings CreateDefaultSettings()
        {
            var settings = new JsonSerializerSettings
            {
                // 等价于 STJ 的 Web 默认（大致思路：常见大小写/空值处理）
                NullValueHandling = NullValueHandling.Ignore,
                // 允许数值从字符串读取可在必要时通过自定义 converter 覆盖到具体字段上
                // Date/Time/Guid 等用 Json.NET 默认
            };
            // 枚举转字符串，并支持 [EnumMember(Value="...")]
            settings.Converters.Add(new StringEnumConverter());
            
            // —— 在此注册你项目里自定义的转换器（替代原 STJ Converter）——
            // 例如：
            // settings.Converters.Add(new RequestIdNewtonsoftConverter());
            // settings.Converters.Add(new ProgressTokenNewtonsoftConverter());
            // settings.Converters.Add(new PrimitiveSchemaDefinitionNewtonsoftConverter());
            // settings.Converters.Add(new ContentBlockNewtonsoftConverter());
            // settings.Converters.Add(new ResourceContentsNewtonsoftConverter());
            //
            // 如果有需要，还可以为 TimeSpan/DateTimeOffset 指定自定义格式的 converter。

            
            return settings;
        }
        /// <summary>
        /// 检查给定的 JSON 是否是合法的 MCP 工具 Schema（必须是对象，且包含 "type":"object"）。
        /// </summary>
        public static bool IsValidMcpToolSchema(JToken token)
        {
            if (token == null) return false;
            if (token.Type != JTokenType.Object) return false;

            var obj = (JObject)token;
            var typeProp = obj["type"];
            return typeProp != null
                   && typeProp.Type == JTokenType.String
                   && string.Equals(typeProp.Value<string>(), "object", StringComparison.Ordinal);
        }
        
        /// <summary>
        /// 返回一个使用默认设置的深拷贝序列化：对象 -> JSON 字符串。
        /// </summary>
        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, DefaultSettings);
        }

        /// <summary>
        /// 返回一个使用默认设置的反序列化：JSON 字符串 -> 对象。
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
        }
    }
}