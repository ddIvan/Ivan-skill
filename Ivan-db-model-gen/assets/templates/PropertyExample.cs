// 单个属性的生成格式示例
// 有注释的字段：
		/// <summary>
    	///文件路径
    	/// </summary>
		public string FileName
		{
			get ;
			set ;
		}		
		

// 无注释的字段：
		public int ID
		{
			get ;
			set ;
		}
		

// 可空值类型字段（如 DateTime?）：
		/// <summary>
    	///创建时间
    	/// </summary>
		public DateTime? CreateTime
		{
			get ;
			set ;
		}		
