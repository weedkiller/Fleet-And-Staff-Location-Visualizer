﻿//-----------------------------------------------------------------------
// <copyright file="Tile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Map
{
	using System;
	using Mapbox.Platform;

	/// <summary>
	///    A Map tile, a square with vector or raster data representing a geographic
	///    bounding box. More info <see href="https://en.wikipedia.org/wiki/Tiled_web_map">
	///    here </see>.
	/// </summary>
	public abstract class Tile
	{

		private CanonicalTileId id;
		private string error;
		private State state = State.New;
		private IAsyncRequest request;
		private Action callback;

		/// <summary> Tile state. </summary>
		public enum State
		{
			/// <summary> New tile, not yet initialized. </summary>
			New,
			/// <summary> Loading data. </summary>
			Loading,
			/// <summary> Data loaded and parsed. </summary>
			Loaded,
			/// <summary> Data loading cancelled. </summary>
			Canceled
		}

		/// <summary> Gets the <see cref="T:Mapbox.Map.CanonicalTileId"/> identifier. </summary>
		/// <value> The canonical tile identifier. </value>
		public CanonicalTileId Id
		{
			get
			{
				return this.id;
			}
			set
			{
				this.id = value;
			}
		}

		/// <summary> Gets the error message if any. </summary>
		/// <value> The error string. </value>
		public string Error
		{
			get
			{
				return this.error;
			}
		}

		/// <summary>
		/// Sets the error message.
		/// </summary>
		/// <param name="errorMessage"></param>
		public void SetError(string errorMessage)
		{
			error = errorMessage;
		}

		/// <summary>
		///     Gets the current state. When fully loaded, you must
		///     check if the data actually arrived and if the tile
		///     is accusing any error.
		/// </summary>
		/// <value> The tile state. </value>
		public State CurrentState
		{
			get
			{
				return this.state;
			}
		}

		/// <summary>
		///     Initializes the <see cref="T:Mapbox.Map.Tile"/> object. It will
		///     start a network request and fire the callback when completed.
		/// </summary>
		/// <param name="param"> Initialization parameters. </param>
		/// <param name="callback"> The completion callback. </param>
		public void Initialize(Parameters param, Action callback)
		{
			this.Cancel();

			this.state = State.Loading;
			this.id = param.Id;
			this.request = param.Fs.Request(this.MakeTileResource(param.MapId).GetUrl(), this.HandleTileResponse);
			this.callback = callback;
		}

		/// <summary>
		///     Returns a <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.Map.Tile"/>.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.String"/> that represents the current
		///     <see cref="T:Mapbox.Map.Tile"/>.
		/// </returns>
		public override string ToString()
		{
			return this.Id.ToString();
		}

		/// <summary>
		///     Cancels the request for the <see cref="T:Mapbox.Map.Tile"/> object.
		///     It will stop a network request and set the tile's state to Canceled.
		/// </summary>
		/// <example>
		/// <code>
		/// // Do not request tiles that we are already requesting
		///	// but at the same time exclude the ones we don't need
		///	// anymore, cancelling the network request.
		///	this.tiles.RemoveWhere((T tile) =>
		///	{
		///		if (cover.Remove(tile.Id))
		///		{
		///			return false;
		///		}
		///		else
		///		{
		///			tile.Cancel();
		///			this.NotifyNext(tile);
		///			return true;			
		/// 	}
		///	});
		/// </code>
		/// </example>
		public void Cancel()
		{
			if (this.request != null)
			{
				this.request.Cancel();
				this.request = null;
			}

			this.state = State.Canceled;
		}

		public void SetState(State state) { this.state = state; }

		// Get the tile resource (raster/vector/etc).
		internal abstract TileResource MakeTileResource(string mapid);

		// Decode the tile.
		internal abstract bool ParseTileData(byte[] data);

		// TODO: Currently the tile decoding is done on the main thread. We must implement
		// a Worker class to abstract this, so on platforms that support threads (like Unity
		// on the desktop, Android, etc) we can use worker threads and when building for
		// the browser, we keep it single-threaded.
		private void HandleTileResponse(Response response)
		{
			if (!string.IsNullOrEmpty(response.Error))
			{
				this.error = response.Error;
			}
			else if (this.ParseTileData(response.Data) == false)
			{
				this.error = "ParseError";
			}

			this.state = State.Loaded;
			this.callback();
		}

		/// <summary>
		///    Parameters for initializing a Tile object.
		/// </summary>
		/// <example>
		/// <code>
		/// var parameters = new Tile.Parameters();
		/// parameters.Fs = MapboxAccess.Instance;
		/// parameters.Id = new CanonicalTileId(_zoom, _tileCoorindateX, _tileCoordinateY);
		/// parameters.MapId = "mapbox.mapbox-streets-v7";
		/// </code>
		/// </example>
		public struct Parameters
		{
			/// <summary> The tile id. </summary>
			public CanonicalTileId Id;

			/// <summary>
			///     The tileset map ID, usually in the format "user.mapid". Exceptionally,
			///     <see cref="T:Mapbox.Map.RasterTile"/> will take the full style URL
			///     from where the tile is composited from, like mapbox://styles/mapbox/streets-v9.
			/// </summary>
			public string MapId;

			/// <summary> The data source abstraction. </summary>
			public IFileSource Fs;
		}
	}
}
