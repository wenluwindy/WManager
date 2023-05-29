using System;
using System.Collections.Generic;

namespace WManager
{
    public static class StringExtension
    {
        /// <summary>
        /// 将输入的string内容按照指定字符分割，并返回分割后的列表，strArray:需要分割的字符串 separator：指定的分割节点字符 举例格式 '\n'
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<string> SplitString(this string strArray, char separator)  //分割字符串的方法
        {
            List<string> lines = new List<string>();   //创建列表以存储输出行
            string currentLine = "";                 //初始化当前行为空

            for (int i = 0; i < strArray.Length; i++)     //遍历每个字符
            {
                if (strArray[i] == separator)                  //如果当前字符是换行符
                {
                    lines.Add(currentLine);         //将当前行添加到列表
                    currentLine = "";               //清空当前行  
                }
                else
                {
                    currentLine += strArray[i];         //否则将字符追加到当前行
                }
            }

            if (currentLine != "") lines.Add(currentLine);   //如果最后一行不为空也加入列表
            return lines;                                  //返回行列表
        }
    }
}