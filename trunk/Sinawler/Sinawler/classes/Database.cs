﻿using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.OracleClient;

/// <summary>
/// 数据库访问基类
/// </summary>
public abstract class Database : IDisposable
{
    /// <summary>
    /// 私有变量，数据库连接串。
    /// </summary>
    protected String _connection_string;

    /// <summary>
    /// 私有变量，数据库类型。
    /// </summary>
    protected String _database_type;

    /// <summary>
    /// 公共属性，数据库类型。
    /// </summary>
    public string DataBaseType
    { get { return _database_type; } }

    /// <summary>
    /// 构造函数。
    /// </summary>
    public Database () { }

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="pDatabaseConnectionString">数据库连接串</param>
    public Database ( string pDatabaseConnectionString ) { }

    /// <summary>
    /// 析构函数，释放非托管资源
    /// </summary>
    ~Database () { }

    /// <summary>
    /// 保护方法，打开数据库连接。
    /// </summary>
    protected abstract void Open ();

    /// <summary>
    /// 公有方法，关闭数据库连接。
    /// </summary>
    public abstract void Close ();

    /// <summary>
    /// 公有方法，释放资源。
    /// </summary>
    public abstract void Dispose ();

    /// <summary>
    /// 公有方法，获取数据，返回一个DataSet。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>DataSet</returns>
    public abstract DataSet GetDataSet ( String SqlString );

    /// <summary>
    /// 公有方法，获取数据，返回一个DataRow。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>DataRow</returns>
    public DataRow GetDataRow ( String SqlString )
    {
        DataSet dataset = GetDataSet( SqlString );
        dataset.CaseSensitive = false;
        if (dataset.Tables[0].Rows.Count > 0)
        {
            return dataset.Tables[0].Rows[0];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 公有方法，执行Sql语句。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>对Update、Insert、Delete为影响到的行数，其他情况为-1</returns>
    public abstract int CountByExecuteSQL ( String SqlString );

    /// <summary>
    /// 公有方法，执行Sql语句。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>对Select为影响到的行数，其他情况为-1</returns>
    public abstract int CountByExecuteSQLSelect ( String SqlString );

    /// <summary>
    /// 公有方法，执行一组Sql语句。
    /// </summary>
    /// <param name="SqlStrings">Sql语句组</param>
    /// <returns>是否成功</returns>
    public abstract bool ExecuteSQL ( ArrayList SqlStrings );

    /// <summary>
    /// 公有方法，在一个数据表中插入一条记录。
    /// </summary>
    /// <param name="TableName">表名</param>
    /// <param name="Cols">哈西表，键值为字段名，值为字段值</param>
    /// <returns>是否成功</returns>
    public bool Insert ( String TableName, Hashtable Cols )
    {
        int Count = 0;

        if (Cols.Count <= 0)
        {
            return true;
        }

        String Fields = " (";
        String Values = " Values(";
        foreach (DictionaryEntry item in Cols)
        {
            if (Count != 0)
            {
                Fields += ",";
                Values += ",";
            }
            Fields += item.Key.ToString();
            Values += item.Value.ToString();
            Count++;
        }
        Fields += ")";
        Values += ")";

        String SqlString = "Insert into " + TableName + Fields + Values;

        if (CountByExecuteSQL( SqlString ) <= 0) return false;

        return true;
    }

    /// <summary>
    /// 公有方法，更新一个数据表。
    /// </summary>
    /// <param name="TableName">表名</param>
    /// <param name="Cols">哈西表，键值为字段名，值为字段值</param>
    /// <param name="Where">Where子句</param>
    /// <returns>是否成功</returns>
    public bool Update ( String TableName, Hashtable Cols, String Where )
    {
        int Count = 0;
        if (Cols.Count <= 0)
        {
            return true;
        }
        String Fields = " ";
        foreach (DictionaryEntry item in Cols)
        {
            if (Count != 0)
            {
                Fields += ",";
            }
            Fields += item.Key.ToString();
            Fields += "=";
            Fields += item.Value.ToString();
            Count++;
        }
        Fields += " ";

        String SqlString = "Update " + TableName + " Set " + Fields + " where " + Where;

        if (CountByExecuteSQL( SqlString ) <= 0) return false;
        return true;
    }

    /// <summary>
    /// 测试数据库连接。若正常，直接结束，否则抛出异常
    /// </summary>
    public abstract void Test ();
}

/// <summary>
/// SQL SERVER 数据库访问类
/// </summary>
public class SqlDatabase : Database
{
    /// <summary>
    /// 私有变量，SQL Server数据库连接。
    /// </summary>
    private SqlConnection _sql_connection;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="DatabaseConnectionString">数据库连接串</param>
    public SqlDatabase ()
    {
        _database_type = "SQL Server";
        SettingItems settings = AppSettings.Load();
        if (settings == null) settings = AppSettings.LoadDefault();
        _connection_string = "Data Source="+settings.DBServer+";User Id="+settings.DBUserName+";Password="+settings.DBPwd+";database="+settings.DBName;
    }

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="pDatabaseConnectionString">数据库连接串</param>
    public SqlDatabase ( string pDatabaseConnectionString )
    {
        _connection_string = pDatabaseConnectionString;
    }

    /// <summary>
    /// 析构函数，释放非托管资源
    /// </summary>
    ~SqlDatabase ()
    {
        try
        {
            if (_sql_connection != null)
                _sql_connection.Close();
        }
        catch { }
        try
        {
            Dispose();
        }
        catch { }
    }

    /// <summary>
    /// 保护方法，打开数据库连接。
    /// </summary>
    protected override void Open ()
    {
        if (_sql_connection == null)
        {
            _sql_connection = new SqlConnection( _connection_string );
        }
        if (_sql_connection.State.Equals( ConnectionState.Closed ))
        {
            _sql_connection.Open();
        }
    }

    /// <summary>
    /// 公有方法，关闭数据库连接。
    /// </summary>
    public override void Close ()
    {
        if (_sql_connection != null && _sql_connection.State.Equals( ConnectionState.Open ))
            _sql_connection.Close();
    }

    /// <summary>
    /// 公有方法，释放资源。
    /// </summary>
    public override void Dispose ()
    {
        // 确保连接被关闭
        if (_sql_connection != null)
        {
            _sql_connection.Dispose();
            _sql_connection = null;
        }
    }

    /// <summary>
    /// 公有方法，获取数据，返回一个DataSet。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>DataSet</returns>
    public override DataSet GetDataSet ( String SqlString )
    {
        DataSet dataset = new DataSet();
        try
        {
            Open();
            SqlDataAdapter adapter = new SqlDataAdapter( SqlString, _sql_connection );
            adapter.Fill( dataset );
            Close();
        }
        catch
        {
            dataset = null;
        }
        return dataset;
    }

    /// <summary>
    /// 公有方法，执行Sql语句。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>对Update、Insert、Delete为影响到的行数，其他情况为-1</returns>
    public override int CountByExecuteSQL ( String SqlString )
    {
        int count = -1;
        Open();
        try
        {
            SqlCommand cmd = new SqlCommand( SqlString, _sql_connection );
            cmd.CommandTimeout = 60;
            count = cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            count = -1;
        }
        finally
        {
            Close();
        }
        return count;
    }

    /// <summary>
    /// 公有方法，执行Sql语句。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>对Select为影响到的行数，其他情况为-1</returns>
    public override int CountByExecuteSQLSelect ( String SqlString )
    {
        int count = -1;
        Open();
        try
        {
            SqlCommand cmd = new SqlCommand( SqlString, _sql_connection );
            cmd.CommandTimeout = 60;
            count = Convert.ToInt32( cmd.ExecuteScalar() );
        }
        catch
        {
            count = -1;
        }
        finally
        {
            Close();
        }
        return count;
    }

    /// <summary>
    /// 公有方法，执行一组Sql语句。
    /// </summary>
    /// <param name="SqlStrings">Sql语句组</param>
    /// <returns>是否成功</returns>
    public override bool ExecuteSQL ( ArrayList SqlStrings )
    {
        bool success = true;
        Open();
        SqlCommand cmd = new SqlCommand();
        SqlTransaction trans = _sql_connection.BeginTransaction();
        cmd.Connection = _sql_connection;
        cmd.Transaction = trans;
        cmd.CommandTimeout = 60;
        try
        {
            foreach (String str in SqlStrings)
            {
                cmd.CommandText = str;
                cmd.ExecuteNonQuery();
            }
            trans.Commit();
        }
        catch
        {
            success = false;
            trans.Rollback();
        }
        finally
        {
            Close();
        }
        return success;
    }

    /// <summary>
    /// 测试数据库连接。若有异常，抛出异常
    /// </summary>
    public override void Test ()
    {
        try
        {
            if (_sql_connection == null)
            {
                _sql_connection = new SqlConnection( _connection_string );
            }
            if (_sql_connection.State.Equals( ConnectionState.Closed ))
            {
                _sql_connection.Open();
            }
        }
        catch
        {
            throw;
        }
        finally
        {
            Close();
            Dispose();
        }
    }
}

/// <summary>
/// Oracle 数据库访问类
/// </summary>
public class OracleDatabase : Database
{
    /// <summary>
    /// 私有变量，Oracle数据库连接。
    /// </summary>
    private OracleConnection _oracle_connection;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="DatabaseConnectionString">数据库连接串</param>
    public OracleDatabase ()
    {
        _database_type = "Oracle";
        SettingItems settings = AppSettings.Load();
        if (settings == null) settings = AppSettings.LoadDefault();
        _connection_string = "Data Source=" + settings.DBServer + ";User Id=" + settings.DBUserName + ";Password=" + settings.DBPwd + ";database=" + settings.DBName;
    }

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="pDatabaseConnectionString">数据库连接串</param>
    public OracleDatabase ( string pDatabaseConnectionString )
    {
        _connection_string = pDatabaseConnectionString;
    }

    /// <summary>
    /// 析构函数，释放非托管资源
    /// </summary>
    ~OracleDatabase ()
    {
        try
        {
            if (_oracle_connection != null)
                _oracle_connection.Close();
        }
        catch { }
        try
        {
            Dispose();
        }
        catch { }
    }

    /// <summary>
    /// 保护方法，打开数据库连接。
    /// </summary>
    protected override void Open ()
    {
        if (_oracle_connection == null)
        {
            _oracle_connection = new OracleConnection( _connection_string );
        }
        if (_oracle_connection.State.Equals( ConnectionState.Closed ))
        {
            _oracle_connection.Open();
        }
    }

    /// <summary>
    /// 公有方法，关闭数据库连接。
    /// </summary>
    public override void Close ()
    {
        if (_oracle_connection != null && _oracle_connection.State.Equals( ConnectionState.Open ))
            _oracle_connection.Close();
    }

    /// <summary>
    /// 公有方法，释放资源。
    /// </summary>
    public override void Dispose ()
    {
        // 确保连接被关闭
        if (_oracle_connection != null)
        {
            _oracle_connection.Dispose();
            _oracle_connection = null;
        }
    }

    /// <summary>
    /// 公有方法，获取数据，返回一个DataSet。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>DataSet</returns>
    public override DataSet GetDataSet ( String SqlString )
    {
        DataSet dataset = new DataSet();
        Open();
        OracleDataAdapter adapter = new OracleDataAdapter( SqlString, _oracle_connection );
        adapter.Fill( dataset );
        Close();
        return dataset;
    }

    /// <summary>
    /// 公有方法，执行Sql语句。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>对Update、Insert、Delete为影响到的行数，其他情况为-1</returns>
    public override int CountByExecuteSQL ( String SqlString )
    {
        int count = -1;
        Open();
        try
        {
            OracleCommand cmd = new OracleCommand( SqlString, _oracle_connection );
            cmd.CommandTimeout = 60;
            count = cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            count = -1;
        }
        finally
        {
            Close();
        }
        return count;
    }

    /// <summary>
    /// 公有方法，执行Sql语句。
    /// </summary>
    /// <param name="SqlString">Sql语句</param>
    /// <returns>对Select为影响到的行数，其他情况为-1</returns>
    public override int CountByExecuteSQLSelect ( String SqlString )
    {
        int count = -1;
        Open();
        try
        {
            OracleCommand cmd = new OracleCommand( SqlString, _oracle_connection );
            cmd.CommandTimeout = 60;
            count = Convert.ToInt32( cmd.ExecuteScalar() );
        }
        catch
        {
            count = -1;
        }
        finally
        {
            Close();
        }
        return count;
    }

    /// <summary>
    /// 公有方法，执行一组Sql语句。
    /// </summary>
    /// <param name="SqlStrings">Sql语句组</param>
    /// <returns>是否成功</returns>
    public override bool ExecuteSQL ( ArrayList SqlStrings )
    {
        bool success = true;
        Open();
        OracleCommand cmd = new OracleCommand();
        OracleTransaction trans = _oracle_connection.BeginTransaction();
        cmd.Connection = _oracle_connection;
        cmd.Transaction = trans;
        cmd.CommandTimeout = 60;
        try
        {
            foreach (String str in SqlStrings)
            {
                cmd.CommandText = str;
                cmd.ExecuteNonQuery();
            }
            trans.Commit();
        }
        catch
        {
            success = false;
            trans.Rollback();
        }
        finally
        {
            Close();
        }
        return success;
    }

    /// <summary>
    /// 测试数据库连接。若有异常，抛出异常
    /// </summary>
    public override void Test ()
    {
        try
        {
            if (_oracle_connection == null)
            {
                _oracle_connection = new OracleConnection( _connection_string );
            }
            if (_oracle_connection.State.Equals( ConnectionState.Closed ))
            {
                _oracle_connection.Open();
            }
        }
        catch
        {
            throw;
        }
        finally
        {
            Close();
            Dispose();
        }
    }
}